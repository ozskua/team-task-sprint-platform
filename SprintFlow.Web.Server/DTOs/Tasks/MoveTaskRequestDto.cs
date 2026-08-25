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
    /// <remarks>
    /// Counted among whatever the caller could see — see <see cref="SprintId"/>.
    /// </remarks>
    [Range(0, int.MaxValue)]
    public int TargetIndex { get; set; }

    /// <summary>
    /// The sprint the caller was looking at, or <c>null</c> when they were looking at the whole
    /// board.
    /// </summary>
    /// <remarks>
    /// Not where the task is being put — a move never changes which sprint a task is in. It says
    /// what <see cref="TargetIndex"/> was counted against.
    /// <para>
    /// Positions run across every task in a column, but a sprint-scoped board only shows some of
    /// them, so "drop it third" means the third <em>visible</em> card and not the third card in the
    /// column. The server translates one into the other. Sending nothing means the caller could see
    /// the whole column, and the index is taken at face value.
    /// </para>
    /// </remarks>
    public int? SprintId { get; set; }
}
