using SprintFlow.Web.Server.DTOs.Teams;

namespace SprintFlow.Web.Server.Interfaces.Services;

public interface ITeamService
{
    /// <summary>
    /// Every team, each with the members currently derived from task assignment.
    /// </summary>
    Task<IReadOnlyList<TeamResponseDto>> GetAllAsync(CancellationToken cancellationToken);
}
