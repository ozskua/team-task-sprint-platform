using System.ComponentModel.DataAnnotations;

namespace SprintFlow.Web.Server.DTOs.Tasks;

/// <summary>
/// Where the caller wants the task to end up. Intent, not arithmetic: the client says which column
/// and which slot, and the server works out every position itself.
/// </summary>
public class MoveTaskRequestDto
{
    /// <summary>
    /// Must be a column of the same board. Any column qualifies — a task can move into and out of
    /// Completed and Cancelled as freely as anywhere else.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int TargetColumnId { get; set; }

    /// <summary>
    /// Zero-based slot in the target column. Larger values are clamped to the end rather than
    /// rejected, because a card dropped past the last one plainly means "put it last".
    /// </summary>
    [Range(0, int.MaxValue)]
    public int TargetIndex { get; set; }
}
