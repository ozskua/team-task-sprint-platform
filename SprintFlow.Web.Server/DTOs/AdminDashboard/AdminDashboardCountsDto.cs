namespace SprintFlow.Web.Server.DTOs.AdminDashboard;

/// <summary>
/// The state of the whole system in six figures.
/// </summary>
/// <remarks>
/// The four task buckets are mutually exclusive and cover every kind of column a board can have, so
/// they always sum to the total number of tasks in the database. Unlike the member dashboard these
/// are system-wide: every task on every board, whoever it belongs to.
/// </remarks>
/// <param name="ToDo">Tasks in a system <c>To Do</c> column.</param>
/// <param name="Open">
/// Tasks in any custom column — Review, Testing, Bench, whatever a board defines. Excludes To Do,
/// Completed and Cancelled.
/// </param>
/// <param name="Completed">Tasks in a system <c>Completed</c> column.</param>
/// <param name="Cancelled">Tasks in a system <c>Cancelled</c> column.</param>
/// <param name="TotalUsers">Every account, active and inactive. A count only — no user data.</param>
/// <param name="TotalBoards">Every board in every area.</param>
public record AdminDashboardCountsDto(
    int ToDo,
    int Open,
    int Completed,
    int Cancelled,
    int TotalUsers,
    int TotalBoards);
