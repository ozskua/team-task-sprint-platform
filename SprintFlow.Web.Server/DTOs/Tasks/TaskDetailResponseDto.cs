using SprintFlow.Web.Server.Common.Enums;

namespace SprintFlow.Web.Server.DTOs.Tasks;

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

    IReadOnlyList<TaskAttachmentResponseDto> Attachments);
