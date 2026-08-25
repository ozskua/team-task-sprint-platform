using SprintFlow.Web.Server.DTOs.Columns;
using SprintFlow.Web.Server.DTOs.Sprints;

namespace SprintFlow.Web.Server.DTOs.Boards;

/// <summary>
/// Everything the board page needs in one response: what the board is, who made it, who is on its
/// team, and the columns with their tasks.
/// </summary>
/// <remarks>
/// The team's id is deliberately absent. A board has exactly one team and nothing a client can do
/// addresses it directly, so exposing the id would invite code that depends on it.
/// <para>
/// <paramref name="CreatedByFullName"/> and <paramref name="TeamMembers"/> answer two different
/// questions and routinely disagree: creating a board records ownership, it does not add anyone to
/// the team. A board with a creator and no members at all is a normal, correct state.
/// </para>
/// </remarks>
/// <param name="BoardId">Identifies the board.</param>
/// <param name="Name">Display name.</param>
/// <param name="AreaId">The Area this board sits in.</param>
/// <param name="CreatedByUserId">Recorded ownership. It confers no permission over the board.</param>
/// <param name="CreatedByFullName">Creator's display name.</param>
/// <param name="CreatedDate">Stored without an offset, so the client renders it as-is.</param>
/// <param name="TeamMembers">
/// Computed from task participation, never stored. Always the whole board's members, even when the
/// page is scoped to a sprint: who is on the team is not a question about one stretch of days, and
/// narrowing it would make people appear to leave the team as sprints are switched.
/// </param>
/// <param name="SelectedSprint">
/// The sprint the tasks below were filtered to, or <c>null</c> when the page is showing the whole
/// board. Echoed back so the page can name what it is showing without matching the id against a
/// list it fetched separately.
/// </param>
/// <param name="Columns">
/// In board order: To Do, then custom columns, then Completed, then Cancelled. The set of columns
/// never depends on the selected sprint — only which tasks sit in them does.
/// </param>
public record BoardDetailResponseDto(
    int BoardId,
    string Name,
    int AreaId,
    int CreatedByUserId,
    string CreatedByFullName,
    DateTime CreatedDate,
    IReadOnlyList<TeamMemberResponseDto> TeamMembers,
    SprintResponseDto? SelectedSprint,
    IReadOnlyList<WorkflowColumnResponseDto> Columns);
