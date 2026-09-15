namespace SprintFlow.Web.Server.DTOs.Boards;

public record BoardSummaryResponseDto(
    int BoardId,
    string BoardName,
    string AreaName,
    int TotalTasks,
    int ToDo,
    int Open,
    int Completed,
    int Cancelled,
    int ProgressPercent,
    int TasksWithoutSprint,
    int ActiveSprints,
    int UpcomingSprints,
    int ArchivedSprints);
