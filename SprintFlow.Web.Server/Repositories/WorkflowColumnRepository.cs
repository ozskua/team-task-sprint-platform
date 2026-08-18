using Microsoft.EntityFrameworkCore;
using SprintFlow.Web.Server.Common.Enums;
using SprintFlow.Web.Server.Data;
using SprintFlow.Web.Server.Interfaces.Repositories;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Repositories;

public class WorkflowColumnRepository : IWorkflowColumnRepository
{
    private readonly SprintFlowDbContext context;

    public WorkflowColumnRepository(SprintFlowDbContext context)
    {
        this.context = context;
    }

    public Task<List<WorkflowColumn>> GetBoardColumnsForUpdateAsync(
        int boardId,
        CancellationToken cancellationToken)
        => context.WorkflowColumns
            // Tracked on purpose: the caller rewrites Position on these instances.
            .Where(c => c.BoardId == boardId)
            // Ties broken by id so the order is deterministic even in the moment between a create
            // and the renumber that follows it.
            .OrderBy(c => c.Position)
            .ThenBy(c => c.WorkflowColumnId)
            .ToListAsync(cancellationToken);

    public Task<WorkflowColumn?> GetForUpdateAsync(int columnId, CancellationToken cancellationToken)
        => context.WorkflowColumns
            .FirstOrDefaultAsync(c => c.WorkflowColumnId == columnId, cancellationToken);

    public Task<WorkflowColumn?> GetByIdAsync(int columnId, CancellationToken cancellationToken)
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

    public Task<bool> HasTasksAsync(int columnId, CancellationToken cancellationToken)
        => context.TaskItems.AnyAsync(t => t.TaskColumnId == columnId, cancellationToken);

    public void Add(WorkflowColumn column) => context.WorkflowColumns.Add(column);

    public void Remove(WorkflowColumn column) => context.WorkflowColumns.Remove(column);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => context.SaveChangesAsync(cancellationToken);
}
