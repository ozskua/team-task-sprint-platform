using SprintFlow.Web.Server.DTOs.Teams;

namespace SprintFlow.Web.Server.DTOs.Dashboard;

/// <summary>
/// One team the caller belongs to — meaning one board where at least one task is assigned to them.
/// </summary>
/// <remarks>
/// A team has no name and no identity of its own; it exists because a board does, one for one. So
/// the board is what names and keys it, and what the client links to. Being a board's creator is
/// not membership: someone who created a board but holds no task on it has no team card here.
/// </remarks>
/// <param name="BoardId">The board this team belongs to. The card's key and link target.</param>
/// <param name="BoardName">That board's name, used as the card's title.</param>
/// <param name="AreaId">The area the board belongs to.</param>
/// <param name="AreaName">That area's name.</param>
/// <param name="MyAssignedTaskCount">
/// Every task assigned to the caller on this board, in <b>any</b> column — To Do, custom, Completed
/// and Cancelled alike. It is deliberately not filtered the way the task list is: this is the same
/// set membership itself is derived from, so it can exceed the number of rows the caller sees under
/// "My Tasks". Naming it "open" or "active" would be untrue.
/// </param>
/// <param name="Members">
/// Everyone on the team, each appearing exactly once however many tasks they hold. Deduplicated in
/// the repository before it gets here.
/// </param>
public record DashboardTeamDto(
    int BoardId,
    string BoardName,
    int AreaId,
    string AreaName,
    int MyAssignedTaskCount,
    IReadOnlyList<TeamMemberResponseDto> Members);
