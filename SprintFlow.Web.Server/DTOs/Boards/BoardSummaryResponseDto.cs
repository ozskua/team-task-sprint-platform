namespace SprintFlow.Web.Server.DTOs.Boards;

// Board-scoped, unlike the Dashboard's system-wide figures. There is deliberately nothing about
// overdue or upcoming work: the schema holds no due date, so those figures have nothing behind
// them. ProgressPercent excludes Cancelled from its denominator.
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
