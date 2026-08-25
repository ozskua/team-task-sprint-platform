using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Interfaces.Repositories;

/// <summary>
/// Reads and writes tasks. Like the column repository, it returns tracked entities and saves once,
/// because moving a task rewrites the positions of every task in one or two columns.
/// </summary>
public interface ITaskRepository
{
    /// <summary>One task, tracked, or <c>null</c> when no such task exists.</summary>
    Task<TaskItem?> GetForUpdateAsync(int taskId, CancellationToken cancellationToken);

    /// <summary>
    /// Which board the task belongs to, or <c>null</c> when no such task exists. Read on its own so
    /// a caller can find out which board to lock without tracking the task first.
    /// </summary>
    Task<int?> GetBoardIdAsync(int taskId, CancellationToken cancellationToken);

    /// <summary>
    /// One task with its creator and participants loaded, read-only. This is the shape every task
    /// response needs.
    /// </summary>
    Task<TaskItem?> GetWithDetailsAsync(int taskId, CancellationToken cancellationToken);

    /// <summary>
    /// A column's tasks in order, tracked so the caller can rewrite their positions.
    /// </summary>
    Task<List<TaskItem>> GetColumnTasksForUpdateAsync(int columnId, CancellationToken cancellationToken);

    /// <summary>
    /// The columns' tasks with creators and participants loaded, read-only. Used to report exactly
    /// the columns a move touched.
    /// </summary>
    Task<List<TaskItem>> GetTasksWithDetailsAsync(IReadOnlyCollection<int> columnIds, CancellationToken cancellationToken);

    /// <summary>How many tasks the column holds — the index a newly created task takes.</summary>
    Task<int> CountInColumnAsync(int columnId, CancellationToken cancellationToken);

    /// <summary>
    /// How many tasks each of a board's sprints holds, keyed by sprint id. One grouped query, so
    /// listing a board's sprints costs the same whether it has two of them or twenty.
    /// </summary>
    /// <remarks>
    /// A sprint holding no tasks is simply absent from the dictionary rather than present with a
    /// zero, and tasks belonging to no sprint are not counted at all.
    /// </remarks>
    Task<IReadOnlyDictionary<int, int>> CountByBoardSprintAsync(
        int boardId,
        CancellationToken cancellationToken);

    /// <summary>
    /// How many tasks every sprint in the system holds, keyed by sprint id. The same grouped query
    /// as <see cref="CountByBoardSprintAsync"/> without the board filter, for the system-wide list.
    /// </summary>
    Task<IReadOnlyDictionary<int, int>> CountBySprintAsync(CancellationToken cancellationToken);

    /// <summary>How many tasks one sprint holds. An aggregate — no task is loaded.</summary>
    Task<int> CountInSprintAsync(int sprintId, CancellationToken cancellationToken);

    /// <summary>
    /// One board's tasks with only their column loaded, read-only — enough to bucket them by status
    /// and see which carry a sprint.
    /// </summary>
    /// <remarks>
    /// Deliberately without the creator or the assignment rows: the board summary counts tasks, it
    /// does not name anybody, and loading people to throw them away would cost two joins per task.
    /// </remarks>
    Task<IReadOnlyList<TaskItem>> GetBoardTasksForSummaryAsync(
        int boardId,
        CancellationToken cancellationToken);

    /// <summary>
    /// One sprint's tasks with their column, creator and assignee loaded, read-only.
    /// </summary>
    /// <remarks>
    /// Ordered the way the board reads — by column, then by position within it — so a sprint's task
    /// list matches the order the same tasks appear in on the board. Ties broken by id.
    /// </remarks>
    Task<IReadOnlyList<TaskItem>> GetBySprintAsync(int sprintId, CancellationToken cancellationToken);

    /// <summary>
    /// Every task assigned to one user, across every board, with the board, its area and the task's
    /// column loaded. Read-only.
    /// </summary>
    /// <remarks>
    /// Assignment, not authorship: a task the user created but is not assigned to is not returned.
    /// Nothing is filtered by column here — the caller decides which of them to show, and the same
    /// rows are what the user's team membership is derived from.
    /// </remarks>
    Task<IReadOnlyList<TaskItem>> GetAssignedToUserAsync(int userId, CancellationToken cancellationToken);

    /// <summary>
    /// Every task in the system, newest first, with its board, that board's area, its column, its
    /// creator and its assignee loaded. Read-only.
    /// </summary>
    /// <remarks>
    /// Unscoped on purpose: this is the admin overview, and nothing in the domain restricts who may
    /// read which board. One query for the whole page — the counts are worked out from these same
    /// rows rather than by asking the database a second time.
    /// </remarks>
    Task<IReadOnlyList<TaskItem>> GetAllWithDetailsAsync(CancellationToken cancellationToken);

    void Add(TaskItem task);

    void Remove(TaskItem task);

    /// <summary>Commits everything staged so far as one transaction.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
