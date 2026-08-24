using Microsoft.EntityFrameworkCore;
using SprintFlow.Web.Server.Data;
using SprintFlow.Web.Server.Interfaces.Repositories;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Repositories;

public class BoardRepository : IBoardRepository
{
    private readonly SprintFlowDbContext context;

    public BoardRepository(SprintFlowDbContext context)
    {
        this.context = context;
    }

    public async Task<int> AddAsync(Board board, CancellationToken cancellationToken)
    {
        context.Boards.Add(board);

        // One SaveChanges for the Board and the Team hanging off it. EF wraps a single save in a
        // transaction, so either both rows are committed or neither is — there is no state where a
        // Board exists without its Team, or a Team without its Board.
        //
        // CreatedDate is left untouched on both: the column defaults supply them, and EF reads the
        // generated values back along with the new keys.
        await context.SaveChangesAsync(cancellationToken);

        return board.BoardId;
    }

    public Task<Board?> GetByIdAsync(int boardId, CancellationToken cancellationToken)
        => context.Boards
            .AsNoTracking()
            .Include(b => b.CreatedByUser) // The creator's name goes into the response.
            .FirstOrDefaultAsync(b => b.BoardId == boardId, cancellationToken);

    public Task<bool> ExistsAsync(int boardId, CancellationToken cancellationToken)
        => context.Boards.AnyAsync(b => b.BoardId == boardId, cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken)
        => context.Boards.CountAsync(cancellationToken);

    public Task<Board?> GetForUpdateAsync(int boardId, CancellationToken cancellationToken)
        => context.Boards.FirstOrDefaultAsync(b => b.BoardId == boardId, cancellationToken);

    // The team, columns, tasks and assignments go with it by cascade. Removing them here as well
    // would duplicate a rule the schema already states, and the two could drift apart.
    public void Remove(Board board) => context.Boards.Remove(board);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => context.SaveChangesAsync(cancellationToken);

    public Task<Board?> GetDetailAsync(int boardId, CancellationToken cancellationToken)
        => context.Boards
            .AsNoTracking()
            .Include(b => b.CreatedByUser)
            .Include(b => b.WorkflowColumns)
                .ThenInclude(c => c.Tasks)
                    .ThenInclude(t => t.CreatedByUser) // Every card shows who created it.
            .Include(b => b.WorkflowColumns)
                .ThenInclude(c => c.Tasks)
                    .ThenInclude(t => t.Participants)
                        .ThenInclude(p => p.User)
            // Ordering is applied when mapping rather than here: EF cannot order an Include chain
            // this deep in one expression, and the mapping has to walk the collections anyway.
            .FirstOrDefaultAsync(b => b.BoardId == boardId, cancellationToken);

    public async Task<TResult> RunBoardOrderingAsync<TResult>(
        int boardId,
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        // The connection is configured to retry transient failures, and a retrying strategy refuses
        // to manage a transaction it did not open itself — so the whole unit is handed to it rather
        // than a transaction being started around it.
        var strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            // A retry re-runs everything below. Starting from a clean tracker means the second
            // attempt reads current rows instead of reusing the half-changed entities the first
            // attempt left behind.
            context.ChangeTracker.Clear();

            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            // The gate. UPDLOCK is incompatible with another UPDLOCK, so a second request for this
            // board blocks here; HOLDLOCK keeps it until the transaction ends, so it blocks until
            // the first request's new positions are committed rather than until this statement
            // finishes. Reading one row is enough — the row is being used as the board's mutex, not
            // for its contents.
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT TOP 1 BoardId FROM Boards WITH (UPDLOCK, HOLDLOCK) WHERE BoardId = {boardId}",
                cancellationToken);

            var result = await operation(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return result;
        });
    }
}
