namespace SprintFlow.Web.Server.DTOs.AdminSprintReport;

/// <summary>
/// One sprint's outcome: how much of it was completed, how much was cancelled, and how much is still
/// open, as counts and as percentages of the sprint's own tasks.
/// </summary>
/// <remarks>
/// The three percentages always total 100. Completed and Cancelled are rounded and Ongoing takes
/// whatever is left, so the two figures the report is read for are exact and the rounding drift lands
/// on the third. A sprint with no tasks reports zero everywhere rather than dividing by zero.
/// </remarks>
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
