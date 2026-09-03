using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SprintFlow.Web.Server.Common.Enums;
using SprintFlow.Web.Server.DTOs.Columns;
using SprintFlow.Web.Server.DTOs.Tasks;
using SprintFlow.Web.Server.Extensions;
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
    /// The task card: everything one task carries, including its comments and its attachments.
    /// </summary>
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

    /// <summary>
    /// Adds a comment to the task. The author is the caller — the request body carries no user id.
    /// </summary>
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

    /// <summary>
    /// Removes a comment, and the files posted with it. Only its author may.
    /// </summary>
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

    /// <summary>
    /// Attaches a file to the task. The bytes are written to the server's attachment folder under a
    /// name the server generates; the database keeps only the metadata.
    /// </summary>
    /// <param name="commentId">
    /// Sent when the file was posted with a comment, so the thread can draw it under that message.
    /// </param>
    [HttpPost("{taskId:int}/attachments")]
    [ProducesResponseType(typeof(TaskAttachmentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskAttachmentResponseDto>> AddAttachment(
        int taskId,
        IFormFile? file,

        // [FromForm] is required: IFormFile is bound from the form automatically, but a simple type
        // is read from the route or the query string unless it is asked for explicitly.
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

    /// <summary>
    /// Downloads one attachment. Addressed through its task, so an id belonging to another task is
    /// simply not found.
    /// </summary>
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

        // Naming the file makes this a download rather than something the browser renders in place.
        return File(
            download.Value.Content,
            download.Value.Metadata.ContentType,
            download.Value.Metadata.FileName);
    }

    /// <summary>Removes an attachment, and the file behind it.</summary>
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
