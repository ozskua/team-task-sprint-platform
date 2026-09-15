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
            .Include(s => s.CreatedByUser)
            .Where(s => s.BoardId == boardId)
            .OrderBy(s => s.StartDate)
            .ThenBy(s => s.SprintId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Sprint>> GetAllWithBoardAsync(CancellationToken cancellationToken)
        => await context.Sprints
            .AsNoTracking()
            .Include(s => s.Board)
                .ThenInclude(b => b.Area)
            .OrderBy(s => s.Board.Area.Name)
            .ThenBy(s => s.Board.Name)
            .ThenBy(s => s.StartDate)
            .ThenBy(s => s.SprintId)
            .ToListAsync(cancellationToken);

    public Task<Sprint?> GetDetailAsync(int sprintId, CancellationToken cancellationToken)
        => context.Sprints
            .AsNoTracking()
            .Include(s => s.Board)
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
            .Include(s => s.CreatedByUser)
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
                     && s.StartDate < endDate
                     && startDate < s.EndDate,
                cancellationToken);

    public void Add(Sprint sprint) => context.Sprints.Add(sprint);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => context.SaveChangesAsync(cancellationToken);
}
