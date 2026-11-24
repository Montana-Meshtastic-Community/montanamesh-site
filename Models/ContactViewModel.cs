using System.ComponentModel.DataAnnotations;
using MimeKit;

namespace MontanaMesh.Web.Models;

public class ContactViewModel : IValidatableObject
{
    [Required, StringLength(100)]
    public string Name { get; set; } = "";

    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = "";

    [Required, StringLength(5000, MinimumLength = 10)]
    public string Message { get; set; } = "";

    public string? Website { get; set; }

    // [EmailAddress] accepts some strings MimeKit cannot use as a Reply-To address.
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Email)) yield break;

        // MimeKit decodes punycode domains, so compare only that one bare mailbox was parsed.
        var parsed = MailboxAddress.TryParse(Email, out var mailbox)
            && string.IsNullOrEmpty(mailbox.Name)
            && mailbox.Route.Count == 0;
        if (!parsed)
        {
            yield return new ValidationResult(
                "Enter one email address, like name@example.com, with no spaces or commas.",
                [nameof(Email)]);
        }
    }
}
