using System.ComponentModel.DataAnnotations;

using SprintFlow.Web.Server.Common.Enums;

namespace SprintFlow.Web.Server.DTOs.Tasks;

public class CreateTaskRequestDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    public TaskPriority Priority { get; set; } = TaskPriority.Normal;

    public int? ColumnId { get; set; }

    public int? AssigneeUserId { get; set; }

    public int? SprintId { get; set; }
}
