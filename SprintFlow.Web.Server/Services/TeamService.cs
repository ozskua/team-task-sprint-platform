using SprintFlow.Web.Server.DTOs.Teams;
using SprintFlow.Web.Server.Interfaces.Repositories;
using SprintFlow.Web.Server.Interfaces.Services;
using SprintFlow.Web.Server.Mappings;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Services;

/// <summary>
/// Reads the teams. There is nothing to create or edit here: a team exists because a board does,
/// and its members exist because tasks are assigned.
/// </summary>
public class TeamService : ITeamService
{
    private readonly ITeamRepository teamRepository;
    private readonly ITaskParticipantRepository participantRepository;

    public TeamService(
        ITeamRepository teamRepository,
        ITaskParticipantRepository participantRepository)
    {
        this.teamRepository = teamRepository;
        this.participantRepository = participantRepository;
    }

    public async Task<IReadOnlyList<TeamResponseDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var teams = await teamRepository.GetAllWithBoardAsync(cancellationToken);

        // One query for every board's members rather than one per team, so the page costs two
        // round trips whatever the number of teams.
        var membersByBoard = await participantRepository.GetMembersByBoardAsync(cancellationToken);

        return teams
            .Select(team => team.ToResponseDto(
                membersByBoard.TryGetValue(team.BoardId, out var members) ? members : []))
            .ToList();
    }
}
