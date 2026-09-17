using SprintFlow.Web.Server.DTOs.Sprints;

namespace SprintFlow.Web.Server.DTOs.AdminSprintReport;

public sealed record SprintReportExport(
    IReadOnlyList<SprintReportRowDto> Rows,
    IReadOnlyDictionary<int, IReadOnlyList<SprintTaskResponseDto>> TasksBySprint,
    DateTime GeneratedAtUtc)
{
    public int TotalTasks => Rows.Sum(row => row.TotalTasks);

    public int CompletedTasks => Rows.Sum(row => row.CompletedCount);

    public int CancelledTasks => Rows.Sum(row => row.CancelledCount);

    public int OngoingTasks => Rows.Sum(row => row.OngoingCount);

    public IReadOnlyList<SprintTaskResponseDto> TasksOf(int sprintId)
        => TasksBySprint.GetValueOrDefault(sprintId, []);

    public static string OutcomeOf(SprintTaskResponseDto task)
        => task.SystemKind switch
        {
            "Completed" => "Completed",
            "Cancelled" => "Cancelled",
            _ => "Ongoing"
        };

    public SprintReportExport ForArea(int areaId)
    {
        var rows = Rows.Where(row => row.AreaId == areaId).ToList();
        var sprintIds = rows.Select(row => row.SprintId).ToHashSet();

        return new SprintReportExport(
            rows,
            TasksBySprint.Where(pair => sprintIds.Contains(pair.Key)).ToDictionary(),
            GeneratedAtUtc);
    }
}
