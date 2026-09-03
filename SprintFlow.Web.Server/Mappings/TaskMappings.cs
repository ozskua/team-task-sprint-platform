using SprintFlow.Web.Server.DTOs.Tasks;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Mappings;

/// <summary>
/// Hand-written entity to DTO mapping, kept in one place so what leaves the server is visible.
/// </summary>
public static class TaskMappings
{
    /// <summary>
    /// Requires <c>Include(t =&gt; t.CreatedByUser)</c>, <c>Include(t =&gt; t.Sprint)</c> and the
    /// assignment row with its user loaded.
    /// </summary>
    public static TaskResponseDto ToResponseDto(this TaskItem task)
        => new(
            task.TaskId,
            task.Title,
            task.Description,
            task.BoardId,
            task.TaskColumnId,
            task.Position,
            task.Priority,
            task.CreatedByUserId,
            task.CreatedByUser.FullName,
            task.CreatedDate,
            // At most one row exists — the unique index on TaskId sees to that — so the first is
            // the assignee, and no row means unassigned.
            task.Participants
                .Select(participant => new TaskAssigneeResponseDto(
                    participant.UserId,
                    participant.User.FullName))
                .FirstOrDefault(),
            task.SprintId,
            // Null both when the task is in no sprint and, defensively, when the navigation was not
            // loaded — the id above is the field callers filter on either way.
            task.Sprint?.Name);

    /// <summary>
    /// Tasks in the order the column shows them. Ties are broken by id so the order never depends
    /// on which row the database happened to return first.
    /// </summary>
    public static IReadOnlyList<TaskResponseDto> ToOrderedResponseDtos(this IEnumerable<TaskItem> tasks)
        => tasks
            .OrderBy(task => task.Position)
            .ThenBy(task => task.TaskId)
            .Select(task => task.ToResponseDto())
            .ToList();

    /// <summary>
    /// The task card. Requires the column, board, sprint, creator, assignment, comments and
    /// attachments to be loaded, each with its user.
    /// </summary>
    public static TaskDetailResponseDto ToDetailResponseDto(this TaskItem task)
        => new(
            task.TaskId,
            task.Title,
            task.Description,
            task.BoardId,
            task.Board.Name,
            task.TaskColumnId,
            task.Column.Name,
            task.Position,
            task.Priority,
            task.CreatedByUserId,
            task.CreatedByUser.FullName,
            task.CreatedDate,
            task.Participants
                .Select(participant => new TaskAssigneeResponseDto(
                    participant.UserId,
                    participant.User.FullName))
                .FirstOrDefault(),
            task.SprintId,
            task.Sprint?.Name,
            task.Comments
                .OrderBy(comment => comment.CreatedDate)
                .ThenBy(comment => comment.TaskCommentId)
                .Select(comment => comment.ToResponseDto())
                .ToList(),
            // Only the loose ones. The rest are drawn under the comment they were posted with.
            task.Attachments
                .Where(attachment => attachment.TaskCommentId is null)
                .OrderBy(attachment => attachment.CreatedDate)
                .ThenBy(attachment => attachment.TaskAttachmentId)
                .Select(attachment => attachment.ToResponseDto())
                .ToList());

    public static TaskCommentResponseDto ToResponseDto(this TaskComment comment)
        => new(
            comment.TaskCommentId,
            comment.UserId,
            comment.User.FullName,
            comment.Content,
            comment.CreatedDate,
            comment.Attachments
                .OrderBy(attachment => attachment.CreatedDate)
                .ThenBy(attachment => attachment.TaskAttachmentId)
                .Select(attachment => attachment.ToResponseDto())
                .ToList());

    public static TaskAttachmentResponseDto ToResponseDto(this TaskAttachment attachment)
        => new(
            attachment.TaskAttachmentId,
            attachment.FileName,
            attachment.ContentType,
            attachment.SizeBytes,
            attachment.UploadedByUserId,
            attachment.UploadedByUser.FullName,
            attachment.CreatedDate);
}
