using System.ComponentModel.DataAnnotations;

namespace SprintFlow.Web.Server.DTOs.Tasks;

/// <summary>
/// What a caller may change about an existing task. Moving it is a separate operation, and the
/// creator is never editable.
/// </summary>
public class UpdateTaskRequestDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional. Sending nothing clears whatever description was there.</summary>
    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>
    /// Who the task is assigned to. <c>null</c> unassigns it, and naming a different user replaces
    /// whoever was there — a task never has two. A named user must exist and be active.
    /// </summary>
    public int? AssigneeUserId { get; set; }
}
