using SprintFlow.Web.Server.DTOs.Columns;

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
/// <param name="TeamMembers">Computed from task participation, never stored.</param>
/// <param name="Columns">In board order: To Do, then custom columns, then Completed, then Cancelled.</param>
public record BoardDetailResponseDto(
    int BoardId,
    string Name,
    int AreaId,
    int CreatedByUserId,
    string CreatedByFullName,
    DateTime CreatedDate,
    IReadOnlyList<TeamMemberResponseDto> TeamMembers,
    IReadOnlyList<WorkflowColumnResponseDto> Columns);
