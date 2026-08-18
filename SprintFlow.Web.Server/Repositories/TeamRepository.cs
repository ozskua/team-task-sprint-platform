using Microsoft.EntityFrameworkCore;
using SprintFlow.Web.Server.Data;
using SprintFlow.Web.Server.Interfaces.Repositories;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Repositories;

public class TeamRepository : ITeamRepository
{
    private readonly SprintFlowDbContext context;

    public TeamRepository(SprintFlowDbContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyList<Team>> GetAllWithBoardAsync(CancellationToken cancellationToken)
        => await context.Teams
            .AsNoTracking()
            .Include(t => t.Board)
                .ThenInclude(b => b.Area)
            .Include(t => t.CreatedByUser)
            // Grouped the way the page reads: areas together, boards alphabetical inside them.
            .OrderBy(t => t.Board.Area.Name)
            .ThenBy(t => t.Board.Name)
            .ThenBy(t => t.TeamId)
            .ToListAsync(cancellationToken);
}
