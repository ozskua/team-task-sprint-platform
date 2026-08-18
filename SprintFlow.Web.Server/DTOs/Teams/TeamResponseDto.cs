namespace SprintFlow.Web.Server.DTOs.Teams;

/// <summary>
/// One team, named by the board it belongs to. A team has no name of its own — it exists because a
/// board does, one for one, so the board's name and area are what identify it to a reader.
/// </summary>
/// <param name="TeamId">Identifies the team.</param>
/// <param name="BoardId">The board this team belongs to.</param>
/// <param name="BoardName">What the board is called.</param>
/// <param name="AreaId">The Area the board sits in.</param>
/// <param name="AreaName">What that Area is called, so two boards sharing a name stay distinguishable.</param>
/// <param name="OwnerUserId">The board's creator, inherited. It grants no permission.</param>
/// <param name="OwnerFullName">That person's display name.</param>
/// <param name="Members">
/// Derived from task assignment, never stored. Empty is a normal state: a board whose tasks are all
/// unassigned has a team with nobody in it, and the owner is not a member unless assigned something.
/// </param>
public record TeamResponseDto(
    int TeamId,
    int BoardId,
    string BoardName,
    int AreaId,
    string AreaName,
    int OwnerUserId,
    string OwnerFullName,
    IReadOnlyList<TeamMemberResponseDto> Members);
