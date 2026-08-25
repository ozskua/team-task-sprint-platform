namespace SprintFlow.Web.Server.DTOs.Sprints;

/// <summary>
/// One sprint, as its own page shows it. The same fields as the list row plus the board's name.
/// </summary>
/// <remarks>
/// The board's name is here and not on <see cref="SprintResponseDto"/> because this page can be
/// opened directly by URL, with nothing on screen yet to say which board the sprint belongs to. A
/// list row is already underneath its board and does not need telling.
/// </remarks>
/// <param name="SprintId">Identifies the sprint.</param>
/// <param name="BoardId">The board this sprint belongs to.</param>
/// <param name="BoardName">That board's display name, so the page can name it without a second call.</param>
/// <param name="Name">Display name.</param>
/// <param name="StartDate">First day of the sprint, included.</param>
/// <param name="EndDate">The day the sprint stops, excluded.</param>
/// <param name="Status"><c>Upcoming</c>, <c>Active</c> or <c>Archived</c>, worked out from today.</param>
/// <param name="TaskCount">How many tasks are currently in this sprint.</param>
/// <param name="CreatedByUserId">Recorded ownership. It confers no permission over the sprint.</param>
/// <param name="CreatedByFullName">Creator's display name.</param>
/// <param name="CreatedDate">When the sprint was created — an audit timestamp, not one of its days.</param>
public record SprintDetailResponseDto(
    int SprintId,
    int BoardId,
    string BoardName,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    int TaskCount,
    int CreatedByUserId,
    string CreatedByFullName,
    DateTime CreatedDate);
