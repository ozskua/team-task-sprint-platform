using SprintFlow.Web.Server.Common.Enums;
using SprintFlow.Web.Server.DTOs.Columns;

namespace SprintFlow.Web.Server.Interfaces.Services;

public interface IWorkflowColumnService
{
    /// <summary>
    /// Adds a custom column to the board, immediately before Completed. Returns <c>null</c> when
    /// no such board exists.
    /// </summary>
    Task<WorkflowColumnResponseDto?> CreateAsync(
        int boardId,
        CreateWorkflowColumnRequestDto request,
        CancellationToken cancellationToken);

    /// <summary>Renames a custom column. System columns are refused.</summary>
    Task<(RenameColumnOutcome Outcome, WorkflowColumnResponseDto? Column)> RenameAsync(
        int columnId,
        RenameWorkflowColumnRequestDto request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes an empty custom column. A system column, or one that still holds tasks, is refused
    /// and nothing is changed.
    /// </summary>
    Task<DeleteColumnOutcome> DeleteAsync(int columnId, CancellationToken cancellationToken);

    /// <summary>
    /// Reorders the board's custom columns. The system columns keep their fixed places whatever
    /// the payload says, and naming one is an error.
    /// </summary>
    Task<(ReorderColumnsOutcome Outcome, IReadOnlyList<WorkflowColumnResponseDto>? Columns)> ReorderAsync(
        int boardId,
        ReorderColumnsRequestDto request,
        CancellationToken cancellationToken);
}
