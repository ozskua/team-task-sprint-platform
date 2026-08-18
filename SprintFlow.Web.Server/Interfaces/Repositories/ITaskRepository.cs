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

    void Add(TaskItem task);

    void Remove(TaskItem task);

    /// <summary>Commits everything staged so far as one transaction.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
