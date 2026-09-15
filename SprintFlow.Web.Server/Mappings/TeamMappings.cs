using SprintFlow.Web.Server.DTOs.Teams;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Mappings;

public static class TeamMappings
{
    public static TeamResponseDto ToResponseDto(this Team team, IReadOnlyList<User> members)
        => new(
            team.TeamId,
            team.BoardId,
            team.Board.Name,
            team.Board.AreaId,
            team.Board.Area.Name,
            team.CreatedByUserId,
            team.CreatedByUser.FullName,
            members
                .Select(member => new TeamMemberResponseDto(member.UserId, member.FullName))
                .ToList());
}
