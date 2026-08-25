namespace SprintFlow.Web.Server.DTOs.Dashboard;

/// <summary>
/// One task assigned to the caller, with enough of its board to be understood away from it.
/// </summary>
/// <remarks>
/// No assignee is carried: every task in this list belongs to the person reading it, so naming them
/// on each row would say nothing.
/// </remarks>
/// <param name="TaskId">The task.</param>
/// <param name="Title">Its title.</param>
/// <param name="BoardId">The board it sits on — also where the client links to.</param>
/// <param name="BoardName">That board's name.</param>
/// <param name="AreaId">The area the board belongs to.</param>
/// <param name="AreaName">That area's name.</param>
/// <param name="ColumnId">The column the task currently sits in.</param>
/// <param name="ColumnName">
/// The column's own name, shown as-is. A board's vocabulary is the user's, so "Bench" is displayed
/// rather than translated into a fixed status word.
/// </param>
/// <param name="SystemKind">
/// <c>ToDo</c>, <c>Completed</c>, <c>Cancelled</c>, or <c>null</c> for a custom column. Sent as the
/// name rather than the number, exactly as the board endpoint does.
/// </param>
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
