namespace SprintFlow.Web.Server.DTOs.Dashboard;

public record DashboardTaskDto(
    int TaskId,
    string Title,
    int BoardId,
    string BoardName,
    int AreaId,
    string AreaName,
    int ColumnId,
    string ColumnName,
    string? SystemKind);
