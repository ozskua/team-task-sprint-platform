namespace SprintFlow.Web.Server.DTOs.Dashboard;

/// <summary>
/// How the caller's assigned tasks are spread across the four kinds of column.
/// </summary>
/// <remarks>
/// The four are mutually exclusive and cover every column a board can have, so they always sum to
/// the caller's total assigned tasks. Only tasks assigned to the caller are counted — never every
/// task on their boards, and never tasks they merely created.
/// </remarks>
/// <param name="ToDo">Tasks in the system <c>To Do</c> column.</param>
/// <param name="Open">
/// Tasks in any custom column — Review, Testing, Blocked, whatever the board defines. Excludes
/// To Do, Completed and Cancelled.
/// </param>
/// <param name="Completed">Tasks in the system <c>Completed</c> column.</param>
/// <param name="Cancelled">Tasks in the system <c>Cancelled</c> column.</param>
public record DashboardTaskCountsDto(
    int ToDo,
    int Open,
    int Completed,
    int Cancelled);
