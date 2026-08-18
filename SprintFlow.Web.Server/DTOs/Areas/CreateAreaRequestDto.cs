using System.ComponentModel.DataAnnotations;

namespace SprintFlow.Web.Server.DTOs.Areas;

/// <summary>
/// Everything a caller may supply when creating an Area — which is the name and nothing else.
/// </summary>
/// <remarks>
/// There is deliberately no <c>CreatedByUserId</c> property. The creator is taken from the caller's
/// token, so a client that sends one is ignored by the model binder rather than rejected by a check
/// someone could forget to write.
/// </remarks>
public class CreateAreaRequestDto
{
    // [Required] also rejects a whitespace-only name: RequiredAttribute trims before testing unless
    // AllowEmptyStrings is set. MaxLength matches the column width.
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
}
