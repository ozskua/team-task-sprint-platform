using Microsoft.EntityFrameworkCore;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Data.Repositories;

public class TaskRepository
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

    public async Task<TaskLocation?> GetLocationAsync(int taskId, CancellationToken cancellationToken)
        => await context.TaskItems
            .AsNoTracking()
            .Where(t => t.TaskId == taskId)
            .Select(t => new TaskLocation(t.BoardId, t.Board.AreaId))
            .FirstOrDefaultAsync(cancellationToken);

    public Task<TaskItem?> GetWithDetailsAsync(int taskId, CancellationToken cancellationToken)
        => context.TaskItems
            .AsNoTracking()
            .Include(t => t.CreatedByUser)
            .Include(t => t.Participants)
                .ThenInclude(p => p.User)
            .Include(t => t.Sprint)
            .FirstOrDefaultAsync(t => t.TaskId == taskId, cancellationToken);

    public Task<List<TaskItem>> GetColumnTasksForUpdateAsync(int columnId, CancellationToken cancellationToken)
        => context.TaskItems
            .Where(t => t.TaskColumnId == columnId)
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
            .Include(t => t.Sprint)
            .Where(t => columnIds.Contains(t.TaskColumnId))
            .OrderBy(t => t.Position)
            .ThenBy(t => t.TaskId)
            .ToListAsync(cancellationToken);

    public Task<int> CountInColumnAsync(int columnId, CancellationToken cancellationToken)
        => context.TaskItems.CountAsync(t => t.TaskColumnId == columnId, cancellationToken);

    public async Task<IReadOnlyDictionary<int, int>> CountByBoardSprintAsync(
        int boardId,
        CancellationToken cancellationToken)
        => await context.TaskItems
            .AsNoTracking()
            .Where(t => t.BoardId == boardId && t.SprintId != null)
            .GroupBy(t => t.SprintId!.Value)
            .Select(group => new { SprintId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.SprintId, row => row.Count, cancellationToken);

    public async Task<IReadOnlyDictionary<int, int>> CountBySprintAsync(
        CancellationToken cancellationToken)
        => await context.TaskItems
            .AsNoTracking()
            .Where(t => t.SprintId != null)
            .GroupBy(t => t.SprintId!.Value)
            .Select(group => new { SprintId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.SprintId, row => row.Count, cancellationToken);

    public Task<int> CountInSprintAsync(int sprintId, CancellationToken cancellationToken)
        => context.TaskItems.CountAsync(t => t.SprintId == sprintId, cancellationToken);

    public async Task<IReadOnlyList<SprintColumnKindCount>> CountBySprintAndColumnKindAsync(
        CancellationToken cancellationToken)
        => await context.TaskItems
            .AsNoTracking()
            .Where(t => t.SprintId != null)
            .GroupBy(t => new { SprintId = t.SprintId!.Value, t.Column.SystemKind })
            .Select(group => new SprintColumnKindCount(
                group.Key.SprintId,
                group.Key.SystemKind,
                group.Count()))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TaskItem>> GetBoardTasksForSummaryAsync(
        int boardId,
        CancellationToken cancellationToken)
        => await context.TaskItems
            .AsNoTracking()
            .Where(t => t.BoardId == boardId)
            .Include(t => t.Column)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TaskItem>> GetBySprintAsync(
        int sprintId,
        CancellationToken cancellationToken)
        => await context.TaskItems
            .AsNoTracking()
            .Where(t => t.SprintId == sprintId)
            .Include(t => t.Column)
            .Include(t => t.CreatedByUser)
            .Include(t => t.Participants)
                .ThenInclude(p => p.User)
            .OrderBy(t => t.Column.Position)
            .ThenBy(t => t.Position)
            .ThenBy(t => t.TaskId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TaskItem>> GetAllInSprintsAsync(CancellationToken cancellationToken)
        => await context.TaskItems
            .AsNoTracking()
            .Where(t => t.SprintId != null)
            .Include(t => t.Column)
            .Include(t => t.CreatedByUser)
            .Include(t => t.Participants)
                .ThenInclude(p => p.User)
            .OrderBy(t => t.SprintId)
            .ThenBy(t => t.Column.Position)
            .ThenBy(t => t.Position)
            .ThenBy(t => t.TaskId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TaskItem>> GetAssignedToUserAsync(
        int userId,
        CancellationToken cancellationToken)
        => await context.TaskItems
            .AsNoTracking()
            .Where(t => t.Participants.Any(p => p.UserId == userId))
            .Include(t => t.Board)
                .ThenInclude(b => b.Area)
            .Include(t => t.Column)
            .OrderBy(t => t.Board.Area.Name)
            .ThenBy(t => t.Board.Name)
            .ThenBy(t => t.Column.Position)
            .ThenBy(t => t.Position)
            .ThenBy(t => t.TaskId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TaskItem>> GetAllWithDetailsAsync(
        CancellationToken cancellationToken)
        => await context.TaskItems
            .AsNoTracking()
            .Include(t => t.Board)
                .ThenInclude(b => b.Area)
            .Include(t => t.Column)
            .Include(t => t.CreatedByUser)
            .Include(t => t.Participants)
                .ThenInclude(p => p.User)
            .OrderByDescending(t => t.CreatedDate)
            .ThenByDescending(t => t.TaskId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TaskColumnKindCount>> CountByColumnKindAsync(
        CancellationToken cancellationToken)
        => await context.TaskItems
            .GroupBy(task => task.Column.SystemKind)
            .Select(group => new TaskColumnKindCount(group.Key, group.Count()))
            .ToListAsync(cancellationToken);

    public void Add(TaskItem task) => context.TaskItems.Add(task);

    public void Remove(TaskItem task) => context.TaskItems.Remove(task);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => context.SaveChangesAsync(cancellationToken);

public async Task<IReadOnlyList<User>> GetBoardMembersAsync(
        int boardId,
        CancellationToken cancellationToken)
        => await context.TaskParticipants
            .AsNoTracking()
            .Where(p => p.Task.BoardId == boardId)
            .Select(p => p.User)
            .Distinct()
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<int>> GetBoardMemberIdsAsync(
        int boardId,
        CancellationToken cancellationToken)
        => await context.TaskParticipants
            .AsNoTracking()
            .Where(p => p.Task.BoardId == boardId && p.User.IsActive)
            .Select(p => p.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<int, IReadOnlyList<User>>> GetMembersByBoardAsync(
        CancellationToken cancellationToken)
    {
        var rows = await context.TaskParticipants
            .AsNoTracking()
            .Select(p => new { p.Task.BoardId, p.User })
            .Distinct()
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => row.BoardId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<User>)group
                    .Select(row => row.User)
                    .OrderBy(user => user.FirstName)
                    .ThenBy(user => user.LastName)
                    .ToList());
    }

    public async Task<IReadOnlyDictionary<int, IReadOnlyList<User>>> GetMembersByBoardsAsync(
        IReadOnlyCollection<int> boardIds,
        CancellationToken cancellationToken)
    {
        if (boardIds.Count == 0)
        {
            return new Dictionary<int, IReadOnlyList<User>>();
        }

        var rows = await context.TaskParticipants
            .AsNoTracking()
            .Where(p => boardIds.Contains(p.Task.BoardId))
            .Select(p => new { p.Task.BoardId, p.User })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => row.BoardId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<User>)group
                    .DistinctBy(row => row.User.UserId)
                    .Select(row => row.User)
                    .OrderBy(user => user.FirstName)
                    .ThenBy(user => user.LastName)
                    .ToList());
    }

    public Task<List<TaskParticipant>> GetParticipantsAsync(int taskId, CancellationToken cancellationToken)
        => context.TaskParticipants
            .Where(p => p.TaskId == taskId)
            .ToListAsync(cancellationToken);

    public void Add(TaskParticipant participant) => context.TaskParticipants.Add(participant);

    public void RemoveRange(IEnumerable<TaskParticipant> participants)
        => context.TaskParticipants.RemoveRange(participants);

public Task<TaskItem?> GetDetailAsync(int taskId, CancellationToken cancellationToken)
        => context.TaskItems
            .AsNoTracking()
            .Include(t => t.Board)
            .Include(t => t.Column)
            .Include(t => t.Sprint)
            .Include(t => t.CreatedByUser)
            .Include(t => t.Participants).ThenInclude(p => p.User)
            .Include(t => t.Comments).ThenInclude(c => c.User)
            .Include(t => t.Comments).ThenInclude(c => c.Attachments).ThenInclude(a => a.UploadedByUser)
            .Include(t => t.Attachments).ThenInclude(a => a.UploadedByUser)
            .FirstOrDefaultAsync(t => t.TaskId == taskId, cancellationToken);

    public Task<bool> ExistsAsync(int taskId, CancellationToken cancellationToken)
        => context.TaskItems.AnyAsync(t => t.TaskId == taskId, cancellationToken);

    public void AddComment(TaskComment comment) => context.TaskComments.Add(comment);

    public Task<TaskComment?> GetCommentForUpdateAsync(int commentId, CancellationToken cancellationToken)
        => context.TaskComments
            .FirstOrDefaultAsync(c => c.TaskCommentId == commentId, cancellationToken);

    public void Remove(TaskComment comment) => context.TaskComments.Remove(comment);

    public Task<bool> CommentExistsOnTaskAsync(
        int commentId,
        int taskId,
        CancellationToken cancellationToken)
        => context.TaskComments.AnyAsync(
            c => c.TaskCommentId == commentId && c.TaskId == taskId,
            cancellationToken);

    public Task<List<TaskAttachment>> GetCommentAttachmentsForUpdateAsync(
        int commentId,
        CancellationToken cancellationToken)
        => context.TaskAttachments
            .Where(a => a.TaskCommentId == commentId)
            .ToListAsync(cancellationToken);

    public void RemoveRange(IEnumerable<TaskAttachment> attachments)
        => context.TaskAttachments.RemoveRange(attachments);

    public Task<TaskComment?> GetCommentAsync(int commentId, CancellationToken cancellationToken)
        => context.TaskComments
            .AsNoTracking()
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.TaskCommentId == commentId, cancellationToken);

    public void AddAttachment(TaskAttachment attachment) => context.TaskAttachments.Add(attachment);

    public Task<TaskAttachment?> GetAttachmentAsync(int attachmentId, CancellationToken cancellationToken)
        => context.TaskAttachments
            .AsNoTracking()
            .Include(a => a.UploadedByUser)
            .FirstOrDefaultAsync(a => a.TaskAttachmentId == attachmentId, cancellationToken);

    public Task<TaskAttachment?> GetAttachmentForUpdateAsync(
        int attachmentId,
        CancellationToken cancellationToken)
        => context.TaskAttachments
            .FirstOrDefaultAsync(a => a.TaskAttachmentId == attachmentId, cancellationToken);

    public void Remove(TaskAttachment attachment) => context.TaskAttachments.Remove(attachment);

    public Task<List<string>> GetAttachmentNamesAsync(int taskId, CancellationToken cancellationToken)
        => context.TaskAttachments
            .AsNoTracking()
            .Where(a => a.TaskId == taskId)
            .Select(a => a.StoredName)
            .ToListAsync(cancellationToken);
}
