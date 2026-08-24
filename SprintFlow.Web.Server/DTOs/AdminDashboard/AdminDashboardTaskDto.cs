using SprintFlow.Web.Server.DTOs.Tasks;

namespace SprintFlow.Web.Server.DTOs.AdminDashboard;

/// <summary>
/// One task, anywhere in the system, with enough of its board to be understood away from it.
/// </summary>
/// <param name="TaskId">The task.</param>
/// <param name="Title">Its title.</param>
/// <param name="BoardId">The board it sits on — also where a row links to.</param>
/// <param name="BoardName">That board's name.</param>
/// <param name="AreaId">The area the board belongs to.</param>
/// <param name="AreaName">That area's name.</param>
/// <param name="ColumnId">The column the task currently sits in.</param>
/// <param name="ColumnName">
/// The column's own name, shown as-is. A board's vocabulary belongs to the people using it, so
/// "Bench" is displayed rather than translated into a fixed status word.
/// </param>
/// <param name="SystemKind">
/// <c>ToDo</c>, <c>Completed</c>, <c>Cancelled</c>, or <c>null</c> for a custom column. Sent as the
/// name rather than the number, exactly as the board endpoint does.
/// </param>
/// <param name="CreatedByFullName">Who wrote the task down. Not the same question as who owes it.</param>
/// <param name="CreatedDate">When it was created. The default sort, newest first.</param>
/// <param name="Assignee">The one person answerable for it, or <c>null</c> when nobody is.</param>
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
