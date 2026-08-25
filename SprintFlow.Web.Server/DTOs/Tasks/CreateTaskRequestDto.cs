using System.ComponentModel.DataAnnotations;

namespace SprintFlow.Web.Server.DTOs.Tasks;

/// <summary>
/// Everything a caller may supply when creating a task: what it says, optionally which column it
/// starts in, and optionally who it is assigned to.
/// </summary>
/// <remarks>
/// There is deliberately no <c>CreatedByUserId</c> and no <c>Position</c>. The creator comes from
/// the caller's token and the position is worked out by the server, so a client that sends either
/// is ignored by the model binder rather than rejected by a check someone could forget to write.
/// The owning board comes from the route.
/// </remarks>
public class CreateTaskRequestDto
{
    // [Required] also rejects a whitespace-only title: RequiredAttribute trims before testing unless
    // AllowEmptyStrings is set. MaxLength matches the column width.
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional. Blank and whitespace are stored as nothing at all.</summary>
    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>
    /// Optional. Left out, the task starts in the board's To Do column — the normal "new task"
    /// flow. Supplied, it must be a column of the board in the route; a column belonging to another
    /// board is rejected and no task is created.
    /// </summary>
    public int? ColumnId { get; set; }

    /// <summary>
    /// Optional. <c>null</c> means unassigned, which is what a new task is unless the caller says
    /// otherwise. A named user must exist and be active.
    /// </summary>
    public int? AssigneeUserId { get; set; }

    /// <summary>
    /// Optional. The sprint to create the task in — normally the one the board is scoped to, so a
    /// task added while looking at a sprint belongs to it rather than appearing to vanish. Left
    /// out, the task belongs to no sprint. A named sprint must belong to the board in the route.
    /// </summary>
    public int? SprintId { get; set; }
}
