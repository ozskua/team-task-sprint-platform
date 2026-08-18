namespace SprintFlow.Web.Server.DTOs.Boards;

/// <summary>
/// A member of the board's team. Worked out at read time from who participates in the board's
/// tasks — there is no membership table, so this list is never stored anywhere.
/// </summary>
/// <param name="UserId">Identifies the member.</param>
/// <param name="FullName">Display name, composed on the server.</param>
public record TeamMemberResponseDto(
    int UserId,
    string FullName);
