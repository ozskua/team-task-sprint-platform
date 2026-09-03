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

    public Task<TaskItem?> GetWithDetailsAsync(int taskId, CancellationToken cancellationToken)
        => context.TaskItems
            .AsNoTracking()
            .Include(t => t.CreatedByUser) // Every task response carries the creator's name.
            .Include(t => t.Participants)  // Zero or one — the task's assignee.
                .ThenInclude(p => p.User)
            .Include(t => t.Sprint)        // Null unless the task is in one; the response names it.
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
            // Tasks in no sprint are excluded rather than grouped under a null key: the caller is
            // filling in a count per sprint, and "not in any sprint" is not one of them.
            .Where(t => t.BoardId == boardId && t.SprintId != null)
            .GroupBy(t => t.SprintId!.Value)
            // One grouped query for the whole list, so a board with twenty sprints still costs one
            // round trip rather than twenty counts.
            .Select(group => new { SprintId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.SprintId, row => row.Count, cancellationToken);

    public async Task<IReadOnlyDictionary<int, int>> CountBySprintAsync(
        CancellationToken cancellationToken)
        => await context.TaskItems
            .AsNoTracking()
            // Tasks in no sprint are excluded rather than grouped under a null key: the caller is
            // filling in a count per sprint, and "not in any sprint" is not one of them.
            .Where(t => t.SprintId != null)
            .GroupBy(t => t.SprintId!.Value)
            // One grouped query for the whole system, so the page costs the same however many
            // boards and sprints exist.
            .Select(group => new { SprintId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.SprintId, row => row.Count, cancellationToken);

    public Task<int> CountInSprintAsync(int sprintId, CancellationToken cancellationToken)
        => context.TaskItems.CountAsync(t => t.SprintId == sprintId, cancellationToken);

    public async Task<IReadOnlyList<TaskItem>> GetBoardTasksForSummaryAsync(
        int boardId,
        CancellationToken cancellationToken)
        => await context.TaskItems
            .AsNoTracking()
            .Where(t => t.BoardId == boardId)
            // The column carries the status; nothing else about the task is counted.
            .Include(t => t.Column)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TaskItem>> GetBySprintAsync(
        int sprintId,
        CancellationToken cancellationToken)
        => await context.TaskItems
            .AsNoTracking()
            .Where(t => t.SprintId == sprintId)
            // The column is what says whether a task is To Do, in a custom column, Completed or
            // Cancelled — the task itself carries no status.
            .Include(t => t.Column)
            .Include(t => t.CreatedByUser)
            // Zero or one — the task's assignee, guaranteed by the unique index on TaskId.
            .Include(t => t.Participants)
                .ThenInclude(p => p.User)
            // The order the board itself reads in, so the list and the kanban agree.
            .OrderBy(t => t.Column.Position)
            .ThenBy(t => t.Position)
            .ThenBy(t => t.TaskId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TaskItem>> GetAssignedToUserAsync(
        int userId,
        CancellationToken cancellationToken)
        => await context.TaskItems
            .AsNoTracking()
            // Assignment, not authorship. The participation row is the only thing consulted, so a
            // task this user wrote but does not hold is not theirs and does not appear.
            .Where(t => t.Participants.Any(p => p.UserId == userId))
            .Include(t => t.Board)
                .ThenInclude(b => b.Area)
            // The column is what says whether a task is To Do, in a custom column, Completed or
            // Cancelled — the task itself carries no status.
            .Include(t => t.Column)
            // Grouped the way the page reads: areas together, boards alphabetical inside them, and
            // within a board the same order as the kanban. Ties broken by id so it never varies.
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
            // The column is what says whether a task is To Do, in a custom column, Completed or
            // Cancelled — the task itself carries no status.
            .Include(t => t.Column)
            .Include(t => t.CreatedByUser)
            // Zero or one — the task's assignee, guaranteed by the unique index on TaskId, so this
            // collection cannot fan the result set out.
            .Include(t => t.Participants)
                .ThenInclude(p => p.User)
            // Newest first: across every board, when a task was created is the only ordering a
            // reader can follow. Ties broken by id so the list never varies between loads.
            .OrderByDescending(t => t.CreatedDate)
            .ThenByDescending(t => t.TaskId)
            .ToListAsync(cancellationToken);

    public void Add(TaskItem task) => context.TaskItems.Add(task);

    // The participation rows go with it, by cascade. Nothing here deletes them one by one, and
    // nothing has to tidy up team membership afterwards — it is derived from those rows.
    public void Remove(TaskItem task) => context.TaskItems.Remove(task);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => context.SaveChangesAsync(cancellationToken);

    // ---- Task participants (assignments) ----

    public async Task<IReadOnlyList<User>> GetBoardMembersAsync(
        int boardId,
        CancellationToken cancellationToken)
        => await context.TaskParticipants
            .AsNoTracking()
            // Joined on the task's board: moving a task between columns never touches BoardId, so
            // no column operation can add or remove a member. Distinct because one user may hold
            // several tasks on a board and is still one member.
            .Where(p => p.Task.BoardId == boardId)
            .Select(p => p.User)
            .Distinct()
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
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
                // One row per assigned task, so a user holding three arrives three times.
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

    // ---- Task card ----

    // Everything the card shows, in one query.
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

    // Tracked, and without the author: this one is read to be deleted, not to be shown.
    public Task<TaskComment?> GetCommentForUpdateAsync(int commentId, CancellationToken cancellationToken)
        => context.TaskComments
            .FirstOrDefaultAsync(c => c.TaskCommentId == commentId, cancellationToken);

    public void Remove(TaskComment comment) => context.TaskComments.Remove(comment);

    /// <summary>Whether this comment exists and belongs to this task — what an upload has to know.</summary>
    public Task<bool> CommentExistsOnTaskAsync(
        int commentId,
        int taskId,
        CancellationToken cancellationToken)
        => context.TaskComments.AnyAsync(
            c => c.TaskCommentId == commentId && c.TaskId == taskId,
            cancellationToken);

    // Tracked, because deleting a comment deletes these rows too: the FK is Restrict, so nothing
    // removes them on our behalf.
    public Task<List<TaskAttachment>> GetCommentAttachmentsForUpdateAsync(
        int commentId,
        CancellationToken cancellationToken)
        => context.TaskAttachments
            .Where(a => a.TaskCommentId == commentId)
            .ToListAsync(cancellationToken);

    public void RemoveRange(IEnumerable<TaskAttachment> attachments)
        => context.TaskAttachments.RemoveRange(attachments);

    // Re-read so the response carries the author's name and the database-generated timestamp.
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

    // Tracked, and without the uploader: this one is read to be deleted, not to be shown.
    public Task<TaskAttachment?> GetAttachmentForUpdateAsync(
        int attachmentId,
        CancellationToken cancellationToken)
        => context.TaskAttachments
            .FirstOrDefaultAsync(a => a.TaskAttachmentId == attachmentId, cancellationToken);

    public void Remove(TaskAttachment attachment) => context.TaskAttachments.Remove(attachment);

    // Read before a task is deleted: the rows cascade, but the files on disk do not.
    public Task<List<string>> GetAttachmentNamesAsync(int taskId, CancellationToken cancellationToken)
        => context.TaskAttachments
            .AsNoTracking()
            .Where(a => a.TaskId == taskId)
            .Select(a => a.StoredName)
            .ToListAsync(cancellationToken);
}
