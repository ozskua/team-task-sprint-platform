using SprintFlow.Web.Server.DTOs.Tasks;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Mappings;

public static class TaskMappings
{
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
            task.Participants
                .Select(participant => new TaskAssigneeResponseDto(
                    participant.UserId,
                    participant.User.FullName))
                .FirstOrDefault(),
            task.SprintId,
            task.Sprint?.Name);

    public static IReadOnlyList<TaskResponseDto> ToOrderedResponseDtos(this IEnumerable<TaskItem> tasks)
        => tasks
            .OrderBy(task => task.Position)
            .ThenBy(task => task.TaskId)
            .Select(task => task.ToResponseDto())
            .ToList();

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
