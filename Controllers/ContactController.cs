using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using MontanaMesh.Web.Models;
using MontanaMesh.Web.Services;

namespace MontanaMesh.Web.Controllers;

public class ContactController(
    IConfiguration configuration,
    IAntiforgery antiforgery,
    ContactSubmissionLimiter limiter,
    ILogger<ContactController> logger) : Controller
{
    // 5,000 multi-byte characters URL-encode to roughly 60 KB, plus name, email, and token.
    private const int MaxRequestBytes = 128 * 1024;
    private const string SendFailedMessage = "Your message could not be sent. Your text is still below; please try again or reach us on Discord.";

    [HttpGetOrHead("/contact")]
    public IActionResult Index() => ContactView(new ContactViewModel());

    [HttpPost("/contact")]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(ValueLengthLimit = MaxRequestBytes, MultipartBodyLengthLimit = MaxRequestBytes)]
    public async Task<IActionResult> Send(ContactViewModel model)
    {
        // Validated here instead of by attribute so an expired form keeps the visitor's text.
        bool tokenValid;
        try
        {
            tokenValid = await antiforgery.IsRequestValidAsync(HttpContext);
        }
        catch (AntiforgeryValidationException exception)
        {
            // The form body could not be read (too large or malformed); there is no text to keep.
            return exception.InnerException is BadHttpRequestException
                ? StatusCode(StatusCodes.Status413PayloadTooLarge)
                : BadRequest();
        }
        if (!tokenValid)
        {
            ModelState.AddModelError("", "This form expired before it was sent. Your text is still below; please press Send again.");
            return ContactView(model, StatusCodes.Status400BadRequest);
        }
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

        var lease = limiter.TryAcquire(HttpContext.Connection.RemoteIpAddress);
        if (lease is null)
        {
            ModelState.AddModelError("", "Too many messages from your network. Please wait ten minutes and try again; your text is still below.");
            return ContactView(model, StatusCodes.Status429TooManyRequests);
        }

        return await DeliverAsync(model, settings, lease);
    }

    private async Task<IActionResult> DeliverAsync(ContactViewModel model, IConfigurationSection settings, ContactSubmissionLimiter.Lease lease)
    {
        try
        {
            using var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(settings["From"]!));
            message.To.Add(MailboxAddress.Parse(settings["To"]!));
            message.ReplyTo.Add(MailboxAddress.Parse(model.Email));
            message.Subject = "Montana Mesh contact message";
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
        catch (Exception exception) when (IsDeliveryFailure(exception))
        {
            // Avoid recording message content, email addresses, or SMTP credentials.
            logger.LogWarning("Contact email delivery failed ({FailureType}).", exception.GetType().Name);
            lease.Release();
            ModelState.AddModelError("", SendFailedMessage);
            return ContactView(model);
        }
    }

    private static bool IsDeliveryFailure(Exception exception) => exception is
        MailKit.CommandException or MailKit.ProtocolException or SslHandshakeException or AuthenticationException or
        ParseException or IOException or System.Net.Sockets.SocketException or FormatException or
        OperationCanceledException or InvalidOperationException or ArgumentException;

    private ViewResult ContactView(ContactViewModel model, int statusCode = StatusCodes.Status200OK)
    {
        ViewData["Title"] = "Contact";
        ViewData["MetaDescription"] = "Get in touch with Montana Mesh about joining the mesh, hosting a node, or a question about the community.";
        Response.Headers.CacheControl = "no-store";
        Response.StatusCode = statusCode;
        return View("Index", model);
    }
}
