using SprintFlow.Web.Server.DTOs.Teams;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Mappings;

public static class TeamMappings
{
    /// <summary>
    /// Requires the board, its area and the owner to be loaded.
    /// </summary>
    /// <param name="team">The team, with its board graph loaded.</param>
    /// <param name="members">
    /// Computed from assignment and passed in, because membership is not part of the team's object
    /// graph — there is no membership table to include.
    /// </param>
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
