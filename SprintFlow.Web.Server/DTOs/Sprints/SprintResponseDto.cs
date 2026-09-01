namespace SprintFlow.Web.Server.DTOs.Sprints;

public record SprintResponseDto(
    int SprintId,
    int BoardId,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    int TaskCount,
    int CreatedByUserId,
    string CreatedByFullName,
    DateTime CreatedDate);
