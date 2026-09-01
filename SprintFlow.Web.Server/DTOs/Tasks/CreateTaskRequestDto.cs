using System.ComponentModel.DataAnnotations;

namespace SprintFlow.Web.Server.DTOs.Tasks;

public class CreateTaskRequestDto
{
    // [Required] also rejects a whitespace-only title: RequiredAttribute trims before testing unless
    // AllowEmptyStrings is set. MaxLength matches the column width.
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    public int? ColumnId { get; set; }

    public int? AssigneeUserId { get; set; }

    public int? SprintId { get; set; }
}
