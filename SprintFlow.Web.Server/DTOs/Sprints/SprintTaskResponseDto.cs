namespace SprintFlow.Web.Server.DTOs.Sprints;

public record SprintTaskResponseDto(
    int TaskId,
    string Title,
    string ColumnName,
    string? SystemKind,
    int? AssigneeUserId,
    string? AssigneeFullName,
    string CreatedByFullName,
    DateTime CreatedDate);
