using Microsoft.EntityFrameworkCore;
using SprintFlow.Web.Server.Common.Enums;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Data.Repositories;

public class BoardRepository
{
    private readonly SprintFlowDbContext context;

    public BoardRepository(SprintFlowDbContext context)
    {
        this.context = context;
    }

public async Task<int> AddAreaAsync(Area area, CancellationToken cancellationToken)
    {
        context.Areas.Add(area);
        await context.SaveChangesAsync(cancellationToken);

        return area.AreaId;
    }

    public Task<Area?> GetAreaAsync(int areaId, CancellationToken cancellationToken)
        => AreasWithRelations().FirstOrDefaultAsync(a => a.AreaId == areaId, cancellationToken);

    public Task<bool> AreaExistsAsync(int areaId, CancellationToken cancellationToken)
        => context.Areas.AnyAsync(a => a.AreaId == areaId, cancellationToken);

    public async Task<IReadOnlyList<Area>> GetAreasAsync(CancellationToken cancellationToken)
        => await AreasWithRelations()
            .OrderByDescending(a => a.CreatedDate)
            .ThenByDescending(a => a.AreaId)
            .ToListAsync(cancellationToken);

    private IQueryable<Area> AreasWithRelations()
        => context.Areas
            .AsNoTracking()
            .Include(a => a.CreatedByUser)
            .Include(a => a.Boards)
                .ThenInclude(b => b.CreatedByUser);

public async Task<int> AddAsync(Board board, CancellationToken cancellationToken)
    {
        context.Boards.Add(board);

        await context.SaveChangesAsync(cancellationToken);

        return board.BoardId;
    }

    public Task<Board?> GetByIdAsync(int boardId, CancellationToken cancellationToken)
        => context.Boards
            .AsNoTracking()
            .Include(b => b.CreatedByUser)
            .FirstOrDefaultAsync(b => b.BoardId == boardId, cancellationToken);

    public Task<bool> ExistsAsync(int boardId, CancellationToken cancellationToken)
        => context.Boards.AnyAsync(b => b.BoardId == boardId, cancellationToken);

    public Task<Board?> GetWithAreaAsync(int boardId, CancellationToken cancellationToken)
        => context.Boards
            .AsNoTracking()
            .Include(b => b.Area)
            .FirstOrDefaultAsync(b => b.BoardId == boardId, cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken)
        => context.Boards.CountAsync(cancellationToken);

    public Task<Board?> GetForUpdateAsync(int boardId, CancellationToken cancellationToken)
        => context.Boards.FirstOrDefaultAsync(b => b.BoardId == boardId, cancellationToken);

    public void Remove(Board board) => context.Boards.Remove(board);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => context.SaveChangesAsync(cancellationToken);

    public Task<Board?> GetDetailAsync(
        int boardId,
        int? sprintId,
        bool allTasks,
        CancellationToken cancellationToken)
        => context.Boards
            .AsNoTracking()
            .Include(b => b.CreatedByUser)
            .Include(b => b.WorkflowColumns)
                .ThenInclude(c => c.Tasks.Where(t => allTasks || (sprintId != null && t.SprintId == sprintId)))
                    .ThenInclude(t => t.CreatedByUser)
            .Include(b => b.WorkflowColumns)
                .ThenInclude(c => c.Tasks.Where(t => allTasks || (sprintId != null && t.SprintId == sprintId)))
                    .ThenInclude(t => t.Participants)
                        .ThenInclude(p => p.User)
            .Include(b => b.WorkflowColumns)
                .ThenInclude(c => c.Tasks.Where(t => allTasks || (sprintId != null && t.SprintId == sprintId)))
                    .ThenInclude(t => t.Sprint)
            .FirstOrDefaultAsync(b => b.BoardId == boardId, cancellationToken);

    public async Task<TResult> RunBoardOrderingAsync<TResult>(
        int boardId,
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        var strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            context.ChangeTracker.Clear();

            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            await context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT TOP 1 BoardId FROM Boards WITH (UPDLOCK, HOLDLOCK) WHERE BoardId = {boardId}",
                cancellationToken);

            var result = await operation(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return result;
        });
    }

public async Task<IReadOnlyList<Team>> GetTeamsAsync(CancellationToken cancellationToken)
        => await context.Teams
            .AsNoTracking()
            .Include(t => t.Board)
                .ThenInclude(b => b.Area)
            .Include(t => t.CreatedByUser)
            .OrderBy(t => t.Board.Area.Name)
            .ThenBy(t => t.Board.Name)
            .ThenBy(t => t.TeamId)
            .ToListAsync(cancellationToken);

public Task<List<WorkflowColumn>> GetBoardColumnsForUpdateAsync(
        int boardId,
        CancellationToken cancellationToken)
        => context.WorkflowColumns
            .Where(c => c.BoardId == boardId)
            .OrderBy(c => c.Position)
            .ThenBy(c => c.WorkflowColumnId)
            .ToListAsync(cancellationToken);

    public Task<WorkflowColumn?> GetColumnForUpdateAsync(int columnId, CancellationToken cancellationToken)
        => context.WorkflowColumns
            .FirstOrDefaultAsync(c => c.WorkflowColumnId == columnId, cancellationToken);

    public Task<WorkflowColumn?> GetColumnAsync(int columnId, CancellationToken cancellationToken)
        => context.WorkflowColumns
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.WorkflowColumnId == columnId, cancellationToken);

    public Task<WorkflowColumn?> GetSystemColumnAsync(
        int boardId,
        SystemColumnKind kind,
        CancellationToken cancellationToken)
        => context.WorkflowColumns
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.BoardId == boardId && c.SystemKind == kind, cancellationToken);

    public Task<bool> ColumnHasTasksAsync(int columnId, CancellationToken cancellationToken)
        => context.TaskItems.AnyAsync(t => t.TaskColumnId == columnId, cancellationToken);

    public void AddColumn(WorkflowColumn column) => context.WorkflowColumns.Add(column);

    public void RemoveColumn(WorkflowColumn column) => context.WorkflowColumns.Remove(column);
}
