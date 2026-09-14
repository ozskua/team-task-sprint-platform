using Microsoft.EntityFrameworkCore;
using SprintFlow.Web.Server.Common.Enums;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Data.Repositories;

/// <summary>
/// Reads and writes the board hierarchy: areas, boards, their one team each, and their columns.
/// </summary>
public class BoardRepository
{
    private readonly SprintFlowDbContext context;

    public BoardRepository(SprintFlowDbContext context)
    {
        this.context = context;
    }

    // ---- Areas ----

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

    // ---- Boards ----

    public async Task<int> AddAsync(Board board, CancellationToken cancellationToken)
    {
        context.Boards.Add(board);

        // One save for the board, its team and its columns: EF wraps a single save in a
        // transaction, so a board can never exist without them.
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

    // The team, columns, tasks and assignments go by cascade; repeating that here could drift
    // from the schema.
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
            // Repeated on every chain because EF refuses a repeated navigation filtered
            // differently. One predicate, three answers: all tasks (backlog), one sprint's tasks
            // (kanban), or none when no sprint is running.
            //
            // Only tasks are filtered, never the columns: a sprint with nothing in Testing still has
            // a Testing column, and that is what keeps one kanban per board.
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
            // Ordering happens in the mapping: EF cannot order an Include chain this deep.
            .FirstOrDefaultAsync(b => b.BoardId == boardId, cancellationToken);

    /// <summary>
    /// Runs <paramref name="operation"/> holding an exclusive lock on the board row, so writes that
    /// depend on reading the board's current ordering cannot interleave.
    /// </summary>
    public async Task<TResult> RunBoardOrderingAsync<TResult>(
        int boardId,
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        // The connection retries transient failures, and a retrying strategy refuses to manage a
        // transaction it did not open — so the whole unit is handed to it.
        var strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            // A retry re-runs everything below, so start from a clean tracker.
            context.ChangeTracker.Clear();

            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            // UPDLOCK blocks a second request for this board; HOLDLOCK keeps the lock until commit,
            // so the loser reads the winner's committed rows. The row is a mutex, not data.
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT TOP 1 BoardId FROM Boards WITH (UPDLOCK, HOLDLOCK) WHERE BoardId = {boardId}",
                cancellationToken);

            var result = await operation(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return result;
        });
    }

    // ---- Teams ----

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

    // ---- Workflow columns ----

    public Task<List<WorkflowColumn>> GetBoardColumnsForUpdateAsync(
        int boardId,
        CancellationToken cancellationToken)
        => context.WorkflowColumns
            // Tracked: the caller rewrites Position on these instances. Ties broken by id so a
            // create and the renumber that follows it agree.
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
