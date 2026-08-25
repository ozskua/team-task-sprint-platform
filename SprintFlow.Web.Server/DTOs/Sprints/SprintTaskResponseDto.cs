namespace SprintFlow.Web.Server.DTOs.Sprints;

/// <summary>
/// One task in a sprint, carrying exactly what the task table draws and nothing else.
/// </summary>
/// <remarks>
/// Narrower than <c>TaskResponseDto</c> on purpose. This shape is read by pages that only display
/// tasks — the sprint's own page and the system-wide sprint list — so it carries no column id,
/// position or board id: nothing there can move a task, and sending the means to would invite code
/// that tries.
/// <para>
/// No board or area name either: both callers already know which board's sprint they opened.
/// </para>
/// </remarks>
/// <param name="TaskId">Identifies the task.</param>
/// <param name="Title">What the task says.</param>
/// <param name="ColumnName">The column holding it — its status, in the board's own words.</param>
/// <param name="SystemKind">
/// <c>ToDo</c>, <c>Completed</c>, <c>Cancelled</c>, or <c>null</c> for a custom column. Drives the
/// status chip's tint, the same way it does on every other screen.
/// </param>
/// <param name="AssigneeFullName">Who is answerable for it, or <c>null</c> when nobody is.</param>
/// <param name="CreatedByFullName">Who wrote it down. Routinely a different person.</param>
/// <param name="CreatedDate">Stored without an offset, so the client renders it as-is.</param>
public record SprintTaskResponseDto(
    int TaskId,
    string Title,
    string ColumnName,
    string? SystemKind,
    string? AssigneeFullName,
    string CreatedByFullName,
    DateTime CreatedDate);
