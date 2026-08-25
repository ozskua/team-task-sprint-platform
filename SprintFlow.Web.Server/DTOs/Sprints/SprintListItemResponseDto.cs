namespace SprintFlow.Web.Server.DTOs.Sprints;

/// <summary>
/// One sprint as the system-wide sprint list shows it: the sprint, plus enough of its board to say
/// where it belongs.
/// </summary>
/// <remarks>
/// Separate from <see cref="SprintResponseDto"/> because that one is always read underneath its own
/// board and needs no board fields, and separate from <see cref="SprintDetailResponseDto"/> because
/// this list is grouped by board and has to distinguish two boards sharing a name.
/// <para>
/// The interval is half-open, as everywhere: <paramref name="StartDate"/> is part of the sprint and
/// <paramref name="EndDate"/> is not.
/// </para>
/// </remarks>
/// <param name="SprintId">Identifies the sprint.</param>
/// <param name="BoardId">The board this sprint belongs to. Sprints are owned by boards, not teams.</param>
/// <param name="BoardName">That board's name.</param>
/// <param name="AreaId">The Area the board sits in.</param>
/// <param name="AreaName">
/// That Area's name. Carried because board names are not unique — two boards called the same thing
/// are told apart by their Area and nothing else.
/// </param>
/// <param name="Name">Display name.</param>
/// <param name="StartDate">First day of the sprint, included. A whole calendar day, no time.</param>
/// <param name="EndDate">The day the sprint stops, excluded. A whole calendar day, no time.</param>
/// <param name="Status"><c>Upcoming</c>, <c>Active</c> or <c>Archived</c>, worked out from today.</param>
/// <param name="TaskCount">How many tasks are currently in this sprint.</param>
public record SprintListItemResponseDto(
    int SprintId,
    int BoardId,
    string BoardName,
    int AreaId,
    string AreaName,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    int TaskCount);
