using Microsoft.EntityFrameworkCore;
using SprintFlow.Web.Server.Data;
using SprintFlow.Web.Server.Interfaces.Repositories;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly SprintFlowDbContext context;

    public TaskRepository(SprintFlowDbContext context)
    {
        this.context = context;
    }

    public Task<TaskItem?> GetForUpdateAsync(int taskId, CancellationToken cancellationToken)
        => context.TaskItems
            .FirstOrDefaultAsync(t => t.TaskId == taskId, cancellationToken);

    public async Task<int?> GetBoardIdAsync(int taskId, CancellationToken cancellationToken)
        => await context.TaskItems
            .AsNoTracking()
            .Where(t => t.TaskId == taskId)
            .Select(t => (int?)t.BoardId)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<TaskItem?> GetWithDetailsAsync(int taskId, CancellationToken cancellationToken)
        => context.TaskItems
            .AsNoTracking()
            .Include(t => t.CreatedByUser) // Every task response carries the creator's name.
            .Include(t => t.Participants)  // Zero or one — the task's assignee.
                .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(t => t.TaskId == taskId, cancellationToken);

    public Task<List<TaskItem>> GetColumnTasksForUpdateAsync(int columnId, CancellationToken cancellationToken)
        => context.TaskItems
            // Tracked on purpose: the caller rewrites Position on these instances.
            .Where(t => t.TaskColumnId == columnId)
            // Ties broken by id, so two tasks that briefly share a position still order the same
            // way every time they are read.
            .OrderBy(t => t.Position)
            .ThenBy(t => t.TaskId)
            .ToListAsync(cancellationToken);

    public Task<List<TaskItem>> GetTasksWithDetailsAsync(
        IReadOnlyCollection<int> columnIds,
        CancellationToken cancellationToken)
        => context.TaskItems
            .AsNoTracking()
            .Include(t => t.CreatedByUser)
            .Include(t => t.Participants)
                .ThenInclude(p => p.User)
            .Where(t => columnIds.Contains(t.TaskColumnId))
            .OrderBy(t => t.Position)
            .ThenBy(t => t.TaskId)
            .ToListAsync(cancellationToken);

    public Task<int> CountInColumnAsync(int columnId, CancellationToken cancellationToken)
        => context.TaskItems.CountAsync(t => t.TaskColumnId == columnId, cancellationToken);

    public void Add(TaskItem task) => context.TaskItems.Add(task);

    // The participation rows go with it, by cascade. Nothing here deletes them one by one, and
    // nothing has to tidy up team membership afterwards — it is derived from those rows.
    public void Remove(TaskItem task) => context.TaskItems.Remove(task);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => context.SaveChangesAsync(cancellationToken);
}
