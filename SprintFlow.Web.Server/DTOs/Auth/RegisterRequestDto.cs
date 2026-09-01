using System.ComponentModel.DataAnnotations;

namespace SprintFlow.Web.Server.DTOs.Auth;

// No role, id or active flag: the server owns all three, so there is nothing here for a client to
// set. The 72-byte password cap is enforced in the service, because MaxLength counts characters
// and bcrypt counts UTF-8 bytes.
public class RegisterRequestDto
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    public string Password { get; set; } = string.Empty;
}
