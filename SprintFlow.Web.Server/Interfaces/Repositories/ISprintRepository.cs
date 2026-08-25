using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Interfaces.Repositories;

/// <summary>
/// Reads and writes sprints. Sprints are never reordered, so unlike the column and task
/// repositories this one has no tracked-for-update read.
/// </summary>
public interface ISprintRepository
{
    /// <summary>
    /// A board's sprints in date order, with their creators loaded. Read-only.
    /// </summary>
    /// <remarks>
    /// Ordered by start day, ties broken by id. Two sprints on one board can never share a start
    /// day — they would overlap — so the tiebreak only matters for the moment before a create is
    /// committed, but it keeps the order deterministic anyway.
    /// </remarks>
    Task<IReadOnlyList<Sprint>> GetBoardSprintsAsync(int boardId, CancellationToken cancellationToken);

    /// <summary>
    /// Every sprint in the system, with its board and that board's area loaded. Read-only.
    /// </summary>
    /// <remarks>
    /// Unscoped on purpose: this is the system-wide overview, and nothing in the domain restricts
    /// who may read which board.
    /// <para>
    /// Ordered area, then board, then start day — the order the page groups them in, so the
    /// grouping needs no sorting of its own. Ties broken by id so the list never varies.
    /// </para>
    /// </remarks>
    Task<IReadOnlyList<Sprint>> GetAllWithBoardAsync(CancellationToken cancellationToken);

    /// <summary>
    /// One sprint with its board and creator loaded, or <c>null</c> when no such sprint exists.
    /// This is the shape the sprint's own page needs.
    /// </summary>
    Task<Sprint?> GetDetailAsync(int sprintId, CancellationToken cancellationToken);

    /// <summary>
    /// One sprint, read-only and without its graph, or <c>null</c>. Used to check that a sprint
    /// exists and which board it belongs to before a task is put into it.
    /// </summary>
    Task<Sprint?> GetByIdAsync(int sprintId, CancellationToken cancellationToken);

    /// <summary>
    /// The board's sprint covering <paramref name="today"/>, or <c>null</c> when none does.
    /// </summary>
    /// <remarks>
    /// At most one can match, and that is a database-backed guarantee rather than an assumption:
    /// sprints on one board may not overlap, so no two of them can contain the same day. There is
    /// therefore nothing to tie-break.
    /// <para>
    /// Half-open, like everywhere else — a sprint whose end day is today is already over.
    /// </para>
    /// </remarks>
    Task<Sprint?> GetActiveAsync(int boardId, DateOnly today, CancellationToken cancellationToken);

    /// <summary>
    /// Whether any sprint on this board already covers part of the same stretch of days.
    /// </summary>
    /// <remarks>
    /// Half-open intervals: two of them overlap when each starts before the other ends. Sprints
    /// that merely touch — one starting on the day another ends — do not overlap, which is the
    /// whole point of excluding the end day.
    /// <para>
    /// <b>Must be called inside <c>RunBoardOrderingAsync</c>.</b> On its own this is a read that
    /// another request can invalidate a moment later; under the board's lock the answer cannot
    /// change before the insert that follows it commits.
    /// </para>
    /// </remarks>
    Task<bool> OverlapsAsync(
        int boardId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken);

    void Add(Sprint sprint);

    /// <summary>Commits everything staged so far as one transaction.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
