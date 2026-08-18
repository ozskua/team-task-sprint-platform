using SprintFlow.Web.Server.Common.Enums;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Interfaces.Repositories;

/// <summary>
/// Reads and writes workflow columns.
/// </summary>
/// <remarks>
/// Several operations rewrite the whole board's positions, so this repository hands back tracked
/// entities and saves once at the end rather than saving inside each method. One
/// <c>SaveChangesAsync</c> is one transaction, so a reorder either lands completely or not at all —
/// the same guarantee Board and Team creation already relies on, without a unit of work.
/// </remarks>
public interface IWorkflowColumnRepository
{
    /// <summary>
    /// The board's columns in board order, tracked so the caller can rewrite their positions.
    /// Empty when the board has no columns or does not exist — the caller checks the board first.
    /// </summary>
    Task<List<WorkflowColumn>> GetBoardColumnsForUpdateAsync(int boardId, CancellationToken cancellationToken);

    /// <summary>One column, tracked, or <c>null</c> when no such column exists.</summary>
    Task<WorkflowColumn?> GetForUpdateAsync(int columnId, CancellationToken cancellationToken);

    /// <summary>One column, read-only, or <c>null</c>. Used to validate a task's target column.</summary>
    Task<WorkflowColumn?> GetByIdAsync(int columnId, CancellationToken cancellationToken);

    /// <summary>
    /// The board's column of a given system kind, read-only. Every board is required to have all
    /// three, so a <c>null</c> here means the data is broken rather than that the caller asked for
    /// something unreasonable.
    /// </summary>
    Task<WorkflowColumn?> GetSystemColumnAsync(int boardId, SystemColumnKind kind, CancellationToken cancellationToken);

    /// <summary>Whether the column still holds any task. Deleting a column that does is refused.</summary>
    Task<bool> HasTasksAsync(int columnId, CancellationToken cancellationToken);

    void Add(WorkflowColumn column);

    void Remove(WorkflowColumn column);

    /// <summary>Commits everything staged so far as one transaction.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
