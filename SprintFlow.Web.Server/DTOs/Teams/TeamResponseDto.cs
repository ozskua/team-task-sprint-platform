namespace SprintFlow.Web.Server.DTOs.Teams;

public record TeamResponseDto(
    int TeamId,
    int BoardId,
    string BoardName,
    int AreaId,
    string AreaName,
    int OwnerUserId,
    string OwnerFullName,
    IReadOnlyList<TeamMemberResponseDto> Members);
