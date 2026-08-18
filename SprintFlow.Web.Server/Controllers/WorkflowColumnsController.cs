using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SprintFlow.Web.Server.Common.Enums;
using SprintFlow.Web.Server.DTOs.Columns;
using SprintFlow.Web.Server.Interfaces.Services;

namespace SprintFlow.Web.Server.Controllers;

/// <summary>
/// Operations on a column that already exists. Creating one and reordering the board are nested
/// under the board instead, because both are really operations on the board as a whole.
/// </summary>
[ApiController]
[Route("api/columns")]
[Authorize]
public class WorkflowColumnsController : ControllerBase
{
    private readonly IWorkflowColumnService workflowColumnService;

    public WorkflowColumnsController(IWorkflowColumnService workflowColumnService)
    {
        this.workflowColumnService = workflowColumnService;
    }

    /// <summary>Renames a custom column. To Do, Completed and Cancelled cannot be renamed.</summary>
    [HttpPut("{columnId:int}")]
    [ProducesResponseType(typeof(WorkflowColumnResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkflowColumnResponseDto>> Rename(
        int columnId,
        [FromBody] RenameWorkflowColumnRequestDto request,
        CancellationToken cancellationToken)
    {
        var (outcome, column) = await workflowColumnService.RenameAsync(columnId, request, cancellationToken);

        return outcome switch
        {
            RenameColumnOutcome.Renamed => Ok(column),

            RenameColumnOutcome.ColumnNotFound => ColumnNotFound(),

            RenameColumnOutcome.ColumnIsSystem => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "System columns cannot be renamed",
                detail: "To Do, Completed and Cancelled keep their names on every board."),

            _ => throw new InvalidOperationException($"Unhandled rename outcome {outcome}.")
        };
    }

    /// <summary>
    /// Deletes an empty custom column. A column that still holds tasks is refused and nothing about
    /// those tasks changes — the caller moves them out first.
    /// </summary>
    [HttpDelete("{columnId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int columnId, CancellationToken cancellationToken)
    {
        var outcome = await workflowColumnService.DeleteAsync(columnId, cancellationToken);

        return outcome switch
        {
            // Nothing moved, so there is nothing for the client to re-read.
            DeleteColumnOutcome.Deleted => NoContent(),

            DeleteColumnOutcome.ColumnNotFound => ColumnNotFound(),

            DeleteColumnOutcome.ColumnIsSystem => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "System columns cannot be deleted",
                detail: "Every board keeps its To Do, Completed and Cancelled columns."),

            DeleteColumnOutcome.ColumnHasTasks => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Column is not empty",
                detail: "Move the column's tasks elsewhere before deleting it. No task was moved or changed."),

            _ => throw new InvalidOperationException($"Unhandled delete outcome {outcome}.")
        };
    }

    private ObjectResult ColumnNotFound()
        => Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Column not found",
            detail: "No column exists with the requested id.");
}
