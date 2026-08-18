using SprintFlow.Web.Server.Common.Enums;
using SprintFlow.Web.Server.DTOs.Columns;
using SprintFlow.Web.Server.DTOs.Tasks;

namespace SprintFlow.Web.Server.Interfaces.Services;

public interface ITaskService
{
    /// <summary>
    /// Creates a task on the board. Without a column id it starts in To Do; with one, the column
    /// must belong to that same board.
    /// </summary>
    /// <param name="createdByUserId">Taken from the caller's token by the controller.</param>
    Task<(CreateTaskOutcome Outcome, TaskResponseDto? Task)> CreateAsync(
        int boardId,
        CreateTaskRequestDto request,
        int createdByUserId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Edits the title and description, and sets who the task is assigned to — naming nobody
    /// unassigns it, and naming someone else replaces whoever was there.
    /// </summary>
    Task<(UpdateTaskOutcome Outcome, TaskResponseDto? Task)> UpdateAsync(
        int taskId,
        UpdateTaskRequestDto request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes the task outright, taking its participation rows with it. <c>false</c> when no such
    /// task exists.
    /// </summary>
    Task<bool> DeleteAsync(int taskId, CancellationToken cancellationToken);

    /// <summary>
    /// Moves the task within its column or to another column of the same board, and returns the
    /// columns the move touched so the caller can reconcile exactly what changed.
    /// </summary>
    Task<(MoveTaskOutcome Outcome, IReadOnlyList<WorkflowColumnResponseDto>? Columns)> MoveAsync(
        int taskId,
        MoveTaskRequestDto request,
        CancellationToken cancellationToken);
}
