using System.ComponentModel.DataAnnotations;

namespace SprintFlow.Web.Server.DTOs.Boards;

public class CreateBoardRequestDto
{
    // [Required] also rejects a whitespace-only name: RequiredAttribute trims before testing unless
    // AllowEmptyStrings is set. MaxLength matches the column width.
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
}
