using SprintFlow.Web.Server.Common.Enums;

namespace SprintFlow.Web.Server.DTOs.Tasks;

public record TaskResponseDto(
    int TaskId,
    string Title,
    string? Description,
    int BoardId,
    int TaskColumnId,
    int Position,
    TaskPriority Priority,
    int CreatedByUserId,
    string CreatedByFullName,
    DateTime CreatedDate,
    TaskAssigneeResponseDto? Assignee,
    int? SprintId,
    string? SprintName);
