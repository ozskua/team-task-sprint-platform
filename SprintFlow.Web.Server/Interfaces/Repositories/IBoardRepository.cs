using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Interfaces.Repositories;

public interface IBoardRepository
{
    /// <summary>
    /// Persists a Board together with whatever is attached to it and returns its generated id.
    /// Saving happens here because the project has no unit of work.
    /// </summary>
    Task<int> AddAsync(Board board, CancellationToken cancellationToken);

    /// <summary>The Board with its creator loaded, or <c>null</c> if no such Board exists.</summary>
    Task<Board?> GetByIdAsync(int boardId, CancellationToken cancellationToken);

    /// <summary>Whether a Board with this id exists.</summary>
    Task<bool> ExistsAsync(int boardId, CancellationToken cancellationToken);

    /// <summary>
    /// The Board with its Area loaded, or <c>null</c>. For pages that title themselves with both —
    /// board names are not unique, so the area is what tells two of them apart.
    /// </summary>
    Task<Board?> GetWithAreaAsync(int boardId, CancellationToken cancellationToken);

    /// <summary>How many Boards exist, across every Area. An aggregate — no Board is loaded.</summary>
    Task<int> CountAsync(CancellationToken cancellationToken);

    /// <summary>One Board, tracked, or <c>null</c>. For renaming and deleting.</summary>
    Task<Board?> GetForUpdateAsync(int boardId, CancellationToken cancellationToken);

    void Remove(Board board);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Everything the board page renders: the creator, the columns in order, and each column's
    /// tasks with their creators and participants. Team members are not here — they are computed
    /// separately, from participation.
    /// </summary>
    /// <param name="sprintId">
    /// Restricts the tasks to that sprint's. Ignored when <paramref name="allTasks"/> is set.
    /// </param>
    /// <param name="allTasks">
    /// Every task on the board, whatever sprint it is in or is not — the backlog's view.
    /// </param>
    /// <remarks>
    /// With neither a sprint nor <paramref name="allTasks"/>, **no** tasks are returned. That is the
    /// kanban of a board with no sprint running: the columns are all there and empty. The columns
    /// themselves are never filtered, so a board keeps its shape whatever is being looked at.
    /// </remarks>
    Task<Board?> GetDetailAsync(
        int boardId,
        int? sprintId,
        bool allTasks,
        CancellationToken cancellationToken);

    /// <summary>
    /// Runs work that reads and rewrites this board's positions, holding an exclusive lock on the
    /// board's row for the whole of it.
    /// </summary>
    /// <remarks>
    /// A transaction on its own is not enough here. Two requests can each read "this column holds
    /// three tasks", each decide the new one belongs at index 3, and each write a 3 — both
    /// transactions commit and the column ends up 0, 1, 2, 3, 3. The lock is what stops the second
    /// request reading the positions before the first has finished changing them; it waits, then
    /// reads 4 rows and writes a 4.
    /// <para>
    /// The lock is taken on the <em>board</em>, not on individual columns, because every ordering
    /// operation belongs to exactly one board — a task can only move between columns of its own
    /// board, so even a two-column move is covered by one lock. Holding exactly one lock per
    /// operation is also what makes deadlock impossible: there is no second resource to wait for,
    /// so no cycle can form and no lock-ordering rule is needed.
    /// </para>
    /// <para>
    /// Anything the operation reads must be read inside it. Anything read beforehand was read
    /// before the lock existed and may already be stale.
    /// </para>
    /// </remarks>
    Task<TResult> RunBoardOrderingAsync<TResult>(
        int boardId,
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken);
}
