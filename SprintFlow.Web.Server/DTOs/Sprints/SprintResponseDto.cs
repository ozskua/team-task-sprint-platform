namespace SprintFlow.Web.Server.DTOs.Sprints;

/// <summary>A sprint as it appears in a board's sprint list.</summary>
/// <remarks>
/// The interval is half-open: <paramref name="StartDate"/> is part of the sprint and
/// <paramref name="EndDate"/> is not, so the last day worked is the day before it. That is what
/// lets one sprint start on the day the previous one ends without the two overlapping.
/// </remarks>
/// <param name="SprintId">Identifies the sprint.</param>
/// <param name="BoardId">The board this sprint belongs to.</param>
/// <param name="Name">Display name. Not unique, like board and area names.</param>
/// <param name="StartDate">First day of the sprint, included. A whole calendar day, no time.</param>
/// <param name="EndDate">The day the sprint stops, excluded. A whole calendar day, no time.</param>
/// <param name="Status">
/// <c>Upcoming</c>, <c>Active</c> or <c>Archived</c>, worked out on the server from today's date.
/// Nothing stores it — a sprint becomes archived by the calendar moving, not by anyone changing it.
/// </param>
/// <param name="TaskCount">How many tasks are currently in this sprint.</param>
/// <param name="CreatedByUserId">Recorded ownership. It confers no permission over the sprint.</param>
/// <param name="CreatedByFullName">Creator's display name.</param>
/// <param name="CreatedDate">When the sprint was created — an audit timestamp, not one of its days.</param>
public record SprintResponseDto(
    int SprintId,
    int BoardId,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    int TaskCount,
    int CreatedByUserId,
    string CreatedByFullName,
    DateTime CreatedDate);
