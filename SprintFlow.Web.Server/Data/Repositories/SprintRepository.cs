using Microsoft.EntityFrameworkCore;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Data.Repositories;

public class SprintRepository
{
    private readonly SprintFlowDbContext context;

    public SprintRepository(SprintFlowDbContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyList<Sprint>> GetBoardSprintsAsync(
        int boardId,
        CancellationToken cancellationToken)
        => await context.Sprints
            .AsNoTracking()
            .Include(s => s.CreatedByUser) // Every row names who created it.
            .Where(s => s.BoardId == boardId)
            .OrderBy(s => s.StartDate)
            .ThenBy(s => s.SprintId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Sprint>> GetAllWithBoardAsync(CancellationToken cancellationToken)
        => await context.Sprints
            .AsNoTracking()
            .Include(s => s.Board)
                .ThenInclude(b => b.Area) // Two boards can share a name; the area tells them apart.
            .OrderBy(s => s.Board.Area.Name)
            .ThenBy(s => s.Board.Name)
            .ThenBy(s => s.StartDate)
            .ThenBy(s => s.SprintId)
            .ToListAsync(cancellationToken);

    public Task<Sprint?> GetDetailAsync(int sprintId, CancellationToken cancellationToken)
        => context.Sprints
            .AsNoTracking()
            .Include(s => s.Board)         // The page names the board it belongs to.
            .Include(s => s.CreatedByUser)
            .FirstOrDefaultAsync(s => s.SprintId == sprintId, cancellationToken);

    public Task<Sprint?> GetByIdAsync(int sprintId, CancellationToken cancellationToken)
        => context.Sprints
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SprintId == sprintId, cancellationToken);

    public Task<Sprint?> GetActiveAsync(
        int boardId,
        DateOnly today,
        CancellationToken cancellationToken)
        => context.Sprints
            .AsNoTracking()
            .Include(s => s.CreatedByUser) // The board page names who created the sprint it shows.
            // Half-open: the start day counts, the end day does not. A sprint ending today has
            // already finished.
            .FirstOrDefaultAsync(
                s => s.BoardId == boardId && s.StartDate <= today && today < s.EndDate,
                cancellationToken);

    public Task<bool> OverlapsAsync(
        int boardId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken)
        => context.Sprints
            .AnyAsync(
                s => s.BoardId == boardId
                     // Each interval starts before the other one ends. With the end day excluded,
                     // an existing sprint ending exactly on startDate fails the first test and one
                     // starting exactly on endDate fails the second, so neither counts as an
                     // overlap — adjacent sprints are allowed, touching sprints are not the same
                     // thing as overlapping ones.
                     && s.StartDate < endDate
                     && startDate < s.EndDate,
                cancellationToken);

    public void Add(Sprint sprint) => context.Sprints.Add(sprint);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => context.SaveChangesAsync(cancellationToken);
}
