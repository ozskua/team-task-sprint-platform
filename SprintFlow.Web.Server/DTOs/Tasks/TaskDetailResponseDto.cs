using SprintFlow.Web.Server.Common.Enums;

namespace SprintFlow.Web.Server.DTOs.Tasks;

// Everything the task card shows in one response, so opening a card costs one request.
// CreatedDate is the task's date: an audit value the database generates and nothing edits.
public record TaskDetailResponseDto(
    int TaskId,
    string Title,
    string? Description,
    int BoardId,
    string BoardName,
    int TaskColumnId,
    string ColumnName,
    int Position,
    TaskPriority Priority,
    int CreatedByUserId,
    string CreatedByFullName,
    DateTime CreatedDate,
    TaskAssigneeResponseDto? Assignee,
    int? SprintId,
    string? SprintName,
    IReadOnlyList<TaskCommentResponseDto> Comments,

    // Only the files that belong to no comment. The rest hang off the comment they were posted
    // with, so the thread can draw each one under its own message and nothing appears twice.
    IReadOnlyList<TaskAttachmentResponseDto> Attachments);
