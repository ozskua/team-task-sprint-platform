namespace SprintFlow.Web.Server.DTOs.Teams;

/// <summary>
/// A member of a team, worked out from task assignment. Nothing about membership is stored, so
/// this list is produced fresh on every read.
/// </summary>
/// <param name="UserId">Identifies the member.</param>
/// <param name="FullName">Display name, composed on the server.</param>
public record TeamMemberResponseDto(
    int UserId,
    string FullName);
