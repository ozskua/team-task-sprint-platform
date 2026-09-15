namespace SprintFlow.Web.Server.DTOs.AdminSprintReport;

public record SprintReportRowDto(
    int SprintId,
    int BoardId,
    string BoardName,
    int AreaId,
    string AreaName,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    int TotalTasks,
    int CompletedCount,
    int CompletedPercent,
    int CancelledCount,
    int CancelledPercent,
    int OngoingCount,
    int OngoingPercent);
