namespace SprintFlow.Web.Server.DTOs.Sprints;

public record SprintListItemResponseDto(
    int SprintId,
    int BoardId,
    string BoardName,
    int AreaId,
    string AreaName,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    int TaskCount);
