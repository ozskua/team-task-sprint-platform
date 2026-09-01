namespace SprintFlow.Web.Server.DTOs.Tasks;

public record TaskResponseDto(
    int TaskId,
    string Title,
    string? Description,
    int BoardId,
    int TaskColumnId,
    int Position,
    int CreatedByUserId,
    string CreatedByFullName,
    DateTime CreatedDate,
    TaskAssigneeResponseDto? Assignee,
    int? SprintId,
    string? SprintName);
