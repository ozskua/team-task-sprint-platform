using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SprintFlow.Web.Server.Common.Enums;
using SprintFlow.Web.Server.DTOs.Columns;
using SprintFlow.Web.Server.DTOs.Tasks;
using SprintFlow.Web.Server.Services;

namespace SprintFlow.Web.Server.Controllers;

/// <summary>
/// Operations on a task that already exists. Creating one is nested under its board, because that
/// is what a new task needs to belong to.
/// </summary>
/// <remarks>
/// Authentication only. Being the task's creator grants nothing: ownership is recorded, not
/// enforced, so any signed-in user may edit, move or delete any task.
/// </remarks>
[ApiController]
[Route("api/tasks")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly TaskService taskService;

    public TasksController(TaskService taskService)
    {
        this.taskService = taskService;
    }

    /// <summary>
    /// Edits what a task says and who it is assigned to. Moving it is a separate operation.
    /// </summary>
    [HttpPut("{taskId:int}")]
    [ProducesResponseType(typeof(TaskResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponseDto>> Update(
        int taskId,
        [FromBody] UpdateTaskRequestDto request,
        CancellationToken cancellationToken)
    {
        var (outcome, task) = await taskService.UpdateAsync(taskId, request, cancellationToken);

        return outcome switch
        {
            UpdateTaskOutcome.Updated => Ok(task),

            UpdateTaskOutcome.TaskNotFound => TaskNotFound(),

            UpdateTaskOutcome.AssigneeUnavailable => AssigneeUnavailable(),

            _ => throw new InvalidOperationException($"Unhandled update outcome {outcome}.")
        };
    }

    /// <summary>
    /// Deletes the task outright. Its participation rows go with it, which may end someone's
    /// membership of the board's team — correctly, since membership is derived from participation.
    /// </summary>
    [HttpDelete("{taskId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int taskId, CancellationToken cancellationToken)
        => await taskService.DeleteAsync(taskId, cancellationToken)
            ? NoContent()
            : TaskNotFound();

    /// <summary>
    /// Moves a task within its column or into another column of the same board. Every column is a
    /// valid destination — Completed and Cancelled hold nothing in place.
    /// </summary>
    [HttpPut("{taskId:int}/move")]
    [ProducesResponseType(typeof(IReadOnlyList<WorkflowColumnResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<WorkflowColumnResponseDto>>> Move(
        int taskId,
        [FromBody] MoveTaskRequestDto request,
        CancellationToken cancellationToken)
    {
        var (outcome, columns) = await taskService.MoveAsync(taskId, request, cancellationToken);

        return outcome switch
        {
            MoveTaskOutcome.Moved => Ok(columns),

            MoveTaskOutcome.TaskNotFound => TaskNotFound(),

            MoveTaskOutcome.ColumnNotFound => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Column not found",
                detail: "No column exists with the requested id."),

            MoveTaskOutcome.ColumnOnDifferentBoard => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Column belongs to a different board",
                detail: "A task can only move between columns of its own board. Nothing was moved."),

            _ => throw new InvalidOperationException($"Unhandled move outcome {outcome}.")
        };
    }

    /// <summary>
    /// Puts the task into one of its board's sprints, or takes it out of whichever one it is in.
    /// </summary>
    /// <remarks>
    /// Separate from editing the task, and from moving it: scheduling a task is neither a change to
    /// what it says nor a change to where it sits on the board.
    /// </remarks>
    [HttpPut("{taskId:int}/sprint")]
    [ProducesResponseType(typeof(TaskResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponseDto>> AssignSprint(
        int taskId,
        [FromBody] AssignTaskSprintRequestDto request,
        CancellationToken cancellationToken)
    {
        var (outcome, task) = await taskService.AssignSprintAsync(taskId, request, cancellationToken);

        return outcome switch
        {
            AssignSprintOutcome.Assigned => Ok(task),

            AssignSprintOutcome.TaskNotFound => TaskNotFound(),

            AssignSprintOutcome.SprintNotFound => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Sprint not found",
                detail: "No sprint exists with the requested id. Send no sprint id to take the task out of its sprint."),

            AssignSprintOutcome.SprintOnDifferentBoard => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Sprint belongs to a different board",
                detail: "A task can only be put into a sprint of its own board. Nothing was changed."),

            _ => throw new InvalidOperationException($"Unhandled assign-sprint outcome {outcome}.")
        };
    }

    private ObjectResult AssigneeUnavailable()
        => Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Assignee unavailable",
            detail: "No active user exists with the requested id. Leave the assignee empty to unassign the task.");

    private ObjectResult TaskNotFound()
        => Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Task not found",
            detail: "No task exists with the requested id.");
}
