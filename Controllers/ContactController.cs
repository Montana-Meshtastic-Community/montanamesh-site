using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MontanaMesh.Web.Models;

namespace MontanaMesh.Web.Controllers;

public class ContactController(IConfiguration configuration, ILogger<ContactController> logger) : Controller
{
    [HttpGet("/contact")]
    public IActionResult Index() => ContactView(new ContactViewModel());

    [HttpPost("/contact")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("contact")]
    [RequestSizeLimit(32768)]
    public async Task<IActionResult> Send(ContactViewModel model)
    {
        if (!string.IsNullOrEmpty(model.Website))
        {
            ModelState.AddModelError("", "Your message could not be sent. Please try again.");
            return ContactView(model);
        }
        if (!ModelState.IsValid) return ContactView(model);

        var settings = configuration.GetSection("Contact");
        if (string.IsNullOrWhiteSpace(settings["SmtpHost"]) ||
            string.IsNullOrWhiteSpace(settings["From"]) || string.IsNullOrWhiteSpace(settings["To"]))
        {
            ModelState.AddModelError("", "The contact form is temporarily unavailable. Please reach us on Discord, or try again later.");
            return ContactView(model);
        }

        try
        {
            using var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(settings["From"]!));
            message.To.Add(MailboxAddress.Parse(settings["To"]!));
            message.ReplyTo.Add(MailboxAddress.Parse(model.Email));
            message.Subject = "MontanaMesh contact message";
            message.MessageId = MimeKit.Utils.MimeUtils.GenerateMessageId("montanamesh.com");
            message.Body = new TextPart("plain") { Text = $"Name: {model.Name}\nEmail: {model.Email}\n\n{model.Message}" };
            using var smtp = new SmtpClient { Timeout = 15000 };
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await smtp.ConnectAsync(settings["SmtpHost"]!, settings.GetValue("SmtpPort", 587),
                settings.GetValue("EnableSsl", true) ? SecureSocketOptions.StartTls : SecureSocketOptions.None, timeout.Token);
            if (!string.IsNullOrWhiteSpace(settings["SmtpUsername"]))
                await smtp.AuthenticateAsync(settings["SmtpUsername"]!, settings["SmtpPassword"] ?? "", timeout.Token);
            await smtp.SendAsync(message, timeout.Token);
            logger.LogInformation("Contact email accepted by SMTP provider. Message-ID: {MessageId}", message.MessageId);
            // Delivery is complete once accepted; disconnect failures must not invite duplicate sends.
            try { await smtp.DisconnectAsync(true, timeout.Token); }
            catch (Exception disconnectException) when (disconnectException is IOException or OperationCanceledException or MailKit.ProtocolException) { }
            TempData["ContactSent"] = true;
            return RedirectToAction(nameof(Index));
        }
        catch (Exception exception) when (exception is MailKit.CommandException or MailKit.ProtocolException or AuthenticationException or IOException or System.Net.Sockets.SocketException or FormatException or OperationCanceledException or InvalidOperationException or ArgumentException)
        {
            // Avoid recording message content, email addresses, or SMTP credentials.
            logger.LogWarning("Contact email delivery failed ({FailureType}).", exception.GetType().Name);
            ModelState.AddModelError("", "Your message could not be sent. Your text is still below; please try again or reach us on Discord.");
            return ContactView(model);
        }
    }

    private ViewResult ContactView(ContactViewModel model)
    {
        ViewData["Title"] = "Contact";
        ViewData["MetaDescription"] = "Get in touch with MontanaMesh about joining the mesh, hosting a node, or a question about the community.";
        ViewData["BackgroundKey"] = "billings";
        Response.Headers.CacheControl = "no-store";
        return View("Index", model);
    }
}
