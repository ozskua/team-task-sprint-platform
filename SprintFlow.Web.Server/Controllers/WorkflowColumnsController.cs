using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SprintFlow.Web.Server.Common.Enums;
using SprintFlow.Web.Server.DTOs.Columns;
using SprintFlow.Web.Server.Services;

namespace SprintFlow.Web.Server.Controllers;

[ApiController]
[Route("api/columns")]
[Authorize]
public class WorkflowColumnsController : ControllerBase
{
    private readonly WorkflowColumnService workflowColumnService;

    public WorkflowColumnsController(WorkflowColumnService workflowColumnService)
    {
        this.workflowColumnService = workflowColumnService;
    }

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
