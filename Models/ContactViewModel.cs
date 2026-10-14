using System.ComponentModel.DataAnnotations;

namespace MontanaMesh.Web.Models;

public class ContactViewModel
{
    [Required, StringLength(100)]
    public string Name { get; set; } = "";

    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = "";

    [Required, StringLength(5000, MinimumLength = 10)]
    public string Message { get; set; } = "";

    public string? Website { get; set; }
}
