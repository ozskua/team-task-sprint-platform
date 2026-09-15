using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SprintFlow.Web.Server.Common.Enums;
using SprintFlow.Web.Server.DTOs.Columns;
using SprintFlow.Web.Server.DTOs.Tasks;
using SprintFlow.Web.Server.Extensions;
using SprintFlow.Web.Server.Services;

namespace SprintFlow.Web.Server.Controllers;

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

    [HttpGet("{taskId:int}")]
    [ProducesResponseType(typeof(TaskDetailResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDetailResponseDto>> GetDetail(
        int taskId,
        CancellationToken cancellationToken)
    {
        var task = await taskService.GetDetailAsync(taskId, cancellationToken);

        return task is null ? TaskNotFound() : Ok(task);
    }

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
        var (outcome, task) = await taskService.UpdateAsync(
            taskId,
            request,
            User.GetAuthenticatedUserId(),
            cancellationToken);

        return outcome switch
        {
            UpdateTaskOutcome.Updated => Ok(task),

            UpdateTaskOutcome.TaskNotFound => TaskNotFound(),

            UpdateTaskOutcome.AssigneeUnavailable => AssigneeUnavailable(),

            _ => throw new InvalidOperationException($"Unhandled update outcome {outcome}.")
        };
    }

    [HttpDelete("{taskId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int taskId, CancellationToken cancellationToken)
        => await taskService.DeleteAsync(taskId, cancellationToken)
            ? NoContent()
            : TaskNotFound();

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
        var (outcome, columns) = await taskService.MoveAsync(
            taskId,
            request,
            User.GetAuthenticatedUserId(),
            cancellationToken);

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

    [HttpPost("{taskId:int}/comments")]
    [ProducesResponseType(typeof(TaskCommentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskCommentResponseDto>> AddComment(
        int taskId,
        [FromBody] CreateTaskCommentRequestDto request,
        CancellationToken cancellationToken)
    {
        var comment = await taskService.AddCommentAsync(
            taskId,
            request,
            User.GetAuthenticatedUserId(),
            cancellationToken);

        return comment is null
            ? TaskNotFound()
            : StatusCode(StatusCodes.Status201Created, comment);
    }

    [HttpDelete("{taskId:int}/comments/{commentId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteComment(
        int taskId,
        int commentId,
        CancellationToken cancellationToken)
    {
        var outcome = await taskService.DeleteCommentAsync(
            taskId,
            commentId,
            User.GetAuthenticatedUserId(),
            cancellationToken);

        return outcome switch
        {
            DeleteCommentOutcome.Deleted => NoContent(),

            DeleteCommentOutcome.NotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Comment not found",
                detail: "No comment exists with the requested id on this task."),

            DeleteCommentOutcome.NotAuthor => Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Comment belongs to someone else",
                detail: "A comment can only be deleted by the person who wrote it."),

            _ => throw new InvalidOperationException($"Unhandled delete-comment outcome {outcome}.")
        };
    }

    [HttpPost("{taskId:int}/attachments")]
    [ProducesResponseType(typeof(TaskAttachmentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskAttachmentResponseDto>> AddAttachment(
        int taskId,
        IFormFile? file,

        [FromForm] int? commentId,
        CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return FileMissing();
        }

        await using var content = file.OpenReadStream();

        var (outcome, attachment) = await taskService.AddAttachmentAsync(
            taskId,
            file.FileName,
            file.Length,
            content,
            User.GetAuthenticatedUserId(),
            commentId,
            cancellationToken);

        return outcome switch
        {
            AddAttachmentOutcome.Added => StatusCode(StatusCodes.Status201Created, attachment),

            AddAttachmentOutcome.TaskNotFound => TaskNotFound(),

            AddAttachmentOutcome.CommentNotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Comment not found",
                detail: "No comment exists with the requested id on this task."),

            AddAttachmentOutcome.FileMissing => FileMissing(),

            AddAttachmentOutcome.FileTooLarge => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "File is too large",
                detail: $"An attachment may be at most {AttachmentStorage.MaxSizeBytes / (1024 * 1024)} MB."),

            AddAttachmentOutcome.FileTypeNotAllowed => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "File type is not allowed",
                detail: "Images, PDF, Office documents, plain text and zip archives can be attached."),

            _ => throw new InvalidOperationException($"Unhandled add-attachment outcome {outcome}.")
        };
    }

    [HttpGet("{taskId:int}/attachments/{attachmentId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadAttachment(
        int taskId,
        int attachmentId,
        CancellationToken cancellationToken)
    {
        var download = await taskService.OpenAttachmentAsync(taskId, attachmentId, cancellationToken);

        if (download is null)
        {
            return AttachmentNotFound();
        }

        return File(
            download.Value.Content,
            download.Value.Metadata.ContentType,
            download.Value.Metadata.FileName);
    }

    [HttpDelete("{taskId:int}/attachments/{attachmentId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAttachment(
        int taskId,
        int attachmentId,
        CancellationToken cancellationToken)
        => await taskService.DeleteAttachmentAsync(taskId, attachmentId, cancellationToken)
            ? NoContent()
            : AttachmentNotFound();

    private ObjectResult FileMissing()
        => Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "No file was sent",
            detail: "Send the file as multipart form data under the field name 'file'.");

    private ObjectResult AttachmentNotFound()
        => Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Attachment not found",
            detail: "No attachment exists with the requested id on this task.");

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
