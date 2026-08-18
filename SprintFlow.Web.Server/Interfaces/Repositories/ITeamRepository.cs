using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Interfaces.Repositories;

public interface ITeamRepository
{
    /// <summary>
    /// Every team with its board, that board's area and the owner loaded. Members are not here —
    /// they are computed from assignment, which is a different query.
    /// </summary>
    Task<IReadOnlyList<Team>> GetAllWithBoardAsync(CancellationToken cancellationToken);
}
