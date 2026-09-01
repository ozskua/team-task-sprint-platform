using SprintFlow.Web.Server.DTOs.Tasks;

namespace SprintFlow.Web.Server.DTOs.AdminDashboard;

public record AdminDashboardTaskDto(
    int TaskId,
    string Title,
    int BoardId,
    string BoardName,
    int AreaId,
    string AreaName,
    int ColumnId,
    string ColumnName,
    string? SystemKind,
    string CreatedByFullName,
    DateTime CreatedDate,
    TaskAssigneeResponseDto? Assignee);
