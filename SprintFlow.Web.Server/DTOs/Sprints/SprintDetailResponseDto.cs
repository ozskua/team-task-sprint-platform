namespace SprintFlow.Web.Server.DTOs.Sprints;

public record SprintDetailResponseDto(
    int SprintId,
    int BoardId,
    string BoardName,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    int TaskCount,
    int CreatedByUserId,
    string CreatedByFullName,
    DateTime CreatedDate);
