using SprintFlow.Web.Server.Common.Enums;
using SprintFlow.Web.Server.Data.Repositories;
using SprintFlow.Web.Server.DTOs.Columns;
using SprintFlow.Web.Server.DTOs.Tasks;
using SprintFlow.Web.Server.Mappings;
using SprintFlow.Web.Server.Messaging;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Services;

public class TaskService
{
    private readonly TaskRepository taskRepository;
    private readonly BoardRepository boardRepository;
    private readonly UserRepository userRepository;
    private readonly SprintRepository sprintRepository;
    private readonly IBoardSummaryCache summaryCache;
    private readonly AttachmentStorage attachmentStorage;
    private readonly BoardEventPublisher boardPublisher;
    private readonly NotificationEventPublisher notificationPublisher;

    public TaskService(
        TaskRepository taskRepository,
        BoardRepository boardRepository,
        UserRepository userRepository,
        SprintRepository sprintRepository,
        IBoardSummaryCache summaryCache,
        AttachmentStorage attachmentStorage,
        BoardEventPublisher boardPublisher,
        NotificationEventPublisher notificationPublisher)
    {
        this.taskRepository = taskRepository;
        this.boardRepository = boardRepository;
        this.userRepository = userRepository;
        this.sprintRepository = sprintRepository;
        this.summaryCache = summaryCache;
        this.attachmentStorage = attachmentStorage;
        this.boardPublisher = boardPublisher;
        this.notificationPublisher = notificationPublisher;
    }

    /// <summary>
    /// Announces that something worth a notification happened, to the people it happened to.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Always called <b>after</b> the change is committed, so nobody is told about work that then
    /// failed to save. The cost of that order is the other way round: a broker that cannot be
    /// reached loses the event while the change stands. That is deliberate for this version —
    /// publishing swallows the failure rather than failing a request that already succeeded — and
    /// it is the dual-write limitation recorded in the roadmap.
    /// </para>
    /// <para>
    /// The recipients are resolved here, at the moment of the change, and travel inside the event.
    /// The consumer never asks who the team is; by the time it runs, the answer could already be
    /// different.
    /// </para>
    /// </remarks>
    private Task PublishNotificationAsync(
        NotificationType type,
        int taskId,
        int actorUserId,
        IReadOnlyList<int> recipientUserIds,
        CancellationToken cancellationToken)
        => notificationPublisher.PublishAsync(
            new NotificationEvent(
                Guid.NewGuid(),
                type,
                taskId,
                actorUserId,
                recipientUserIds,
                DateTime.UtcNow),
            cancellationToken);

    /// <summary>
    /// Everyone on the task's board except whoever acted.
    /// </summary>
    /// <remarks>
    /// "Team" is not re-invented: it is the membership rule already in the project — anyone holding
    /// a task on this board. The query returns each member once and leaves out deactivated
    /// accounts, so no duplicate and no unreadable recipient can reach the event, and removing the
    /// actor here is what stops anyone being told about their own action.
    /// </remarks>
    private async Task<IReadOnlyList<int>> TeamExceptActorAsync(
        int boardId,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var memberIds = await taskRepository.GetBoardMemberIdsAsync(boardId, cancellationToken);

        return memberIds.Where(memberId => memberId != actorUserId).ToList();
    }

    /// <summary>
    /// Tells the other clients watching this board to re-read it.
    /// </summary>
    /// <remarks>
    /// Separate from cache invalidation on purpose, and not always paired with it: the summary
    /// reports counts, while the board shows a task's title, priority and assignee. An edit changes
    /// what is on screen without changing a single figure the summary holds, so it publishes here
    /// and still, correctly, does not drop the cache entry.
    /// <para>
    /// The hub itself now lives in the realtime service, so this announces over the broker instead
    /// of calling <c>IHubContext</c> directly. The five call sites, the reason strings and what the
    /// browser finally receives are all unchanged — only the path between them is.
    /// </para>
    /// </remarks>
    private Task NotifyBoardChangedAsync(int boardId, string reason, CancellationToken cancellationToken)
        => boardPublisher.PublishAsync(boardId, reason, cancellationToken);

    public async Task<(CreateTaskOutcome Outcome, TaskResponseDto? Task)> CreateAsync(
        int boardId,
        CreateTaskRequestDto request,
        int createdByUserId,
        CancellationToken cancellationToken)
    {
        if (!await boardRepository.ExistsAsync(boardId, cancellationToken))
        {
            return (CreateTaskOutcome.BoardNotFound, null);
        }

        // Under the board's lock: the new task's position is derived from how many tasks the
        // column already holds, and two requests reading that count together would pick the same
        // index.
        var (outcome, taskId) = await boardRepository.RunBoardOrderingAsync(
            boardId,
            async token =>
            {
                var (resolution, column) = await ResolveStartingColumnAsync(boardId, request.ColumnId, token);

                if (column is null)
                {
                    return (resolution, (int?)null);
                }

                // Checked before anything is written, so a bad assignee leaves no half-made task.
                if (!await IsAssignableAsync(request.AssigneeUserId, token))
                {
                    return (CreateTaskOutcome.AssigneeUnavailable, null);
                }

                // Same for the sprint: the composite foreign key would refuse one from another
                // board anyway, and checking here makes it a clear 400.
                var sprintCheck = await CheckSprintAsync(request.SprintId, boardId, token);

                if (sprintCheck is CreateTaskOutcome problem)
                {
                    return (problem, null);
                }

                // Positions are contiguous from zero, so the count is the next free slot.
                var position = await taskRepository.CountInColumnAsync(column.WorkflowColumnId, token);

                var task = new TaskItem
                {
                    BoardId = boardId,
                    TaskColumnId = column.WorkflowColumnId,
                    Position = position,

                    Title = request.Title.Trim(),
                    Description = Normalise(request.Description),
                    Priority = request.Priority,

                    // From the caller's token; no request DTO property can influence it.
                    CreatedByUserId = createdByUserId,

                    // The sprint the board is scoped to, so a task added while looking at one
                    // belongs to it rather than seeming to disappear. Null for the whole board.
                    SprintId = request.SprintId

                    // CreatedDate is intentionally not set; the database default supplies it.
                };

                taskRepository.Add(task);

                if (request.AssigneeUserId is int assignee)
                {
                    // One save writes both, so a task is never briefly visible unassigned.
                    task.Participants.Add(new TaskParticipant { UserId = assignee });
                }

                await taskRepository.SaveChangesAsync(token);

                return (CreateTaskOutcome.Created, task.TaskId);
            },
            cancellationToken);

        if (taskId is null)
        {
            return (outcome, null);
        }

        await summaryCache.RemoveAsync(boardId, cancellationToken);

        await NotifyBoardChangedAsync(boardId, "taskCreated", cancellationToken);

        // After the commit: the task exists before anyone is told. The actor is never their own
        // recipient — being told about a task you just assigned yourself says nothing.
        if (request.AssigneeUserId is int assignee && assignee != createdByUserId)
        {
            await PublishNotificationAsync(
                NotificationType.TaskAssigned,
                taskId.Value,
                createdByUserId,
                [assignee],
                cancellationToken);
        }

        // Read back outside the lock: the row is committed, and assembling a response is not work
        // the board needs to be held for.
        return (outcome, await ReadBackAsync(taskId.Value, cancellationToken));
    }

    /// <param name="actorUserId">
    /// Who is editing, from their token. Needed because nobody is told about their own action, so
    /// an assignment has to know who made it.
    /// </param>
    public async Task<(UpdateTaskOutcome Outcome, TaskResponseDto? Task)> UpdateAsync(
        int taskId,
        UpdateTaskRequestDto request,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var boardId = await taskRepository.GetBoardIdAsync(taskId, cancellationToken);

        if (boardId is null)
        {
            return (UpdateTaskOutcome.TaskNotFound, null);
        }

        // Under the board's lock: replacing an assignee is a delete then an insert on a uniquely
        // indexed column, so two edits must not interleave.
        var (outcome, assigneeChanged) = await boardRepository.RunBoardOrderingAsync(
            boardId.Value,
            async token =>
            {
                var task = await taskRepository.GetForUpdateAsync(taskId, token);

                if (task is null)
                {
                    return (UpdateTaskOutcome.TaskNotFound, false);
                }

                if (!await IsAssignableAsync(request.AssigneeUserId, token))
                {
                    return (UpdateTaskOutcome.AssigneeUnavailable, false);
                }

                task.Title = request.Title.Trim();
                task.Description = Normalise(request.Description);
                task.Priority = request.Priority;

                // Decided inside the lock: an edit that renames a task and leaves its assignee
                // alone must not tell that person they have been given it again.
                var changed = await ReplaceAssigneeAsync(taskId, request.AssigneeUserId, token);

                // Creator, column and position are untouched: editing what a task says is not
                // moving it, and ownership is never editable.
                await taskRepository.SaveChangesAsync(token);

                return (UpdateTaskOutcome.Updated, changed);
            },
            cancellationToken);

        // The board summary is deliberately not invalidated: it counts tasks by column and by
        // whether they carry a sprint, and none of the fields this changes affect either. Dropping
        // the entry would throw away a correct answer on every text edit.
        if (outcome != UpdateTaskOutcome.Updated)
        {
            return (outcome, null);
        }

        // The cards do show all four, though, so the other clients are told even though the cache
        // keeps its entry.
        await NotifyBoardChangedAsync(boardId.Value, "taskUpdated", cancellationToken);

        // Only when the assignment moved, only to somebody, and never to whoever moved it.
        if (assigneeChanged && request.AssigneeUserId is int assignee && assignee != actorUserId)
        {
            await PublishNotificationAsync(
                NotificationType.TaskAssigned,
                taskId,
                actorUserId,
                [assignee],
                cancellationToken);
        }

        return (outcome, await ReadBackAsync(taskId, cancellationToken));
    }

    /// <summary>
    /// <c>null</c> means unassigned, which is always allowed. A named user has to exist and still
    /// be able to sign in.
    /// </summary>
    private async Task<bool> IsAssignableAsync(int? assigneeUserId, CancellationToken cancellationToken)
        => assigneeUserId is null
           || await userRepository.IsActiveAsync(assigneeUserId.Value, cancellationToken);

    /// <summary>
    /// Leaves the task with exactly the assignment asked for: none, or the one named. Removing
    /// first and adding after is what makes "replace" a single idea rather than two operations a
    /// caller could get half-way through.
    /// </summary>
    /// <returns>
    /// Whether the assignment actually moved. Already recognised here, because the early return
    /// below is precisely the "nothing changed" case; the caller uses it to decide whether anyone
    /// needs telling.
    /// </returns>
    private async Task<bool> ReplaceAssigneeAsync(
        int taskId,
        int? assigneeUserId,
        CancellationToken cancellationToken)
    {
        var existing = await taskRepository.GetParticipantsAsync(taskId, cancellationToken);

        // Already the right person: leave the row so AddedDate is not reset by a no-op edit.
        if (assigneeUserId is int wanted && existing.Count == 1 && existing[0].UserId == wanted)
        {
            return false;
        }

        // Unassigned before and unassigned after: nothing to write, and nothing happened.
        if (assigneeUserId is null && existing.Count == 0)
        {
            return false;
        }

        if (existing.Count > 0)
        {
            taskRepository.RemoveRange(existing);
        }

        if (assigneeUserId is int assignee)
        {
            taskRepository.Add(new TaskParticipant { TaskId = taskId, UserId = assignee });
        }

        return true;
    }

    public async Task<(AssignSprintOutcome Outcome, TaskResponseDto? Task)> AssignSprintAsync(
        int taskId,
        AssignTaskSprintRequestDto request,
        CancellationToken cancellationToken)
    {
        // No board lock, unlike the other writes: this changes one column on one row and touches
        // no position and no unique index, so the later write simply wins.
        var task = await taskRepository.GetForUpdateAsync(taskId, cancellationToken);

        if (task is null)
        {
            return (AssignSprintOutcome.TaskNotFound, null);
        }

        if (request.SprintId is int requested)
        {
            var sprint = await sprintRepository.GetByIdAsync(requested, cancellationToken);

            if (sprint is null)
            {
                return (AssignSprintOutcome.SprintNotFound, null);
            }

            // The composite foreign key would refuse this too; checking first makes it a 400.
            if (sprint.BoardId != task.BoardId)
            {
                return (AssignSprintOutcome.SprintOnDifferentBoard, null);
            }
        }

        // Null takes the task out of its sprint. Column and position are untouched either way.
        task.SprintId = request.SprintId;

        await taskRepository.SaveChangesAsync(cancellationToken);

        // Changes how many of the board's tasks are in no sprint, which the summary reports.
        await summaryCache.RemoveAsync(task.BoardId, cancellationToken);

        await NotifyBoardChangedAsync(task.BoardId, "taskSprintChanged", cancellationToken);

        return (AssignSprintOutcome.Assigned, await ReadBackAsync(taskId, cancellationToken));
    }

    public async Task<bool> DeleteAsync(int taskId, CancellationToken cancellationToken)
    {
        // Read without the lock only to learn which board to lock; decided again inside.
        var boardId = await taskRepository.GetBoardIdAsync(taskId, cancellationToken);

        if (boardId is null)
        {
            return false;
        }

        // Read before the delete, because the rows go by cascade and the files on disk do not.
        var attachmentNames = await taskRepository.GetAttachmentNamesAsync(taskId, cancellationToken);

        // Under the board's lock: deleting renumbers the survivors.
        var deleted = await boardRepository.RunBoardOrderingAsync(
            boardId.Value,
            async token =>
            {
                var task = await taskRepository.GetForUpdateAsync(taskId, token);

                if (task is null)
                {
                    return false;
                }

                // Read before the delete so the survivors can be renumbered in the same save.
                var columnTasks = await taskRepository.GetColumnTasksForUpdateAsync(task.TaskColumnId, token);

                taskRepository.Remove(task);
                columnTasks.RemoveAll(candidate => candidate.TaskId == taskId);
                Renumber(columnTasks);

                // Participation rows go by cascade, and team membership is derived from them.
                await taskRepository.SaveChangesAsync(token);

                return true;
            },
            cancellationToken);

        if (deleted)
        {
            // Comment and attachment rows went by cascade; the files did not. Removed after the
            // commit, because a deleted file cannot be rolled back.
            foreach (var storedName in attachmentNames)
            {
                attachmentStorage.Delete(storedName);
            }

            await summaryCache.RemoveAsync(boardId.Value, cancellationToken);

            await NotifyBoardChangedAsync(boardId.Value, "taskDeleted", cancellationToken);
        }

        return deleted;
    }

    /// <param name="actorUserId">
    /// Who is moving it, from their token. Needed because the team is told when a task is finished
    /// or cancelled, and the person who did it is not told about their own action.
    /// </param>
    public async Task<(MoveTaskOutcome Outcome, IReadOnlyList<WorkflowColumnResponseDto>? Columns)> MoveAsync(
        int taskId,
        MoveTaskRequestDto request,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        // Read without the lock only to learn which board to lock. A task belonging to a different
        // board is rejected inside, and nothing is written.
        var targetColumn = await boardRepository.GetColumnAsync(request.TargetColumnId, cancellationToken);

        if (targetColumn is null)
        {
            return (MoveTaskOutcome.ColumnNotFound, null);
        }

        // Under the board's lock. Both columns a move touches belong to the same board, so one
        // lock covers both — no second lock, no lock order to get wrong, no deadlock.
        var (outcome, affectedColumnIds) = await boardRepository.RunBoardOrderingAsync(
            targetColumn.BoardId,
            async token =>
            {
                var task = await taskRepository.GetForUpdateAsync(taskId, token);

                if (task is null)
                {
                    return (MoveTaskOutcome.TaskNotFound, (List<int>?)null);
                }

                // A task may move into and out of every column of its own board — To Do, custom,
                // Completed and Cancelled alike. The one thing it may not do is leave the board,
                // and the composite foreign key would refuse that anyway; this check is what makes
                // it a clear 400.
                if (targetColumn.BoardId != task.BoardId)
                {
                    return (MoveTaskOutcome.ColumnOnDifferentBoard, null);
                }

                var sourceColumnId = task.TaskColumnId;
                var targetColumnId = targetColumn.WorkflowColumnId;

                var sourceTasks = await taskRepository.GetColumnTasksForUpdateAsync(sourceColumnId, token);
                sourceTasks.RemoveAll(candidate => candidate.TaskId == taskId);

                // Same column means one list, not two: taking the task out and putting it back at a
                // different index is exactly a reorder.
                var targetTasks = sourceColumnId == targetColumnId
                    ? sourceTasks
                    : await taskRepository.GetColumnTasksForUpdateAsync(targetColumnId, token);

                // The caller counted only the cards they could see, which on a sprint-scoped board
                // is some of the column. Translated here into a position in the whole one.
                var index = ToColumnIndex(targetTasks, request.TargetIndex, request.SprintId);

                task.TaskColumnId = targetColumnId;
                targetTasks.Insert(index, task);

                Renumber(sourceTasks);

                if (!ReferenceEquals(sourceTasks, targetTasks))
                {
                    Renumber(targetTasks);
                }

                // One save for both columns, so the board is never left half-renumbered.
                await taskRepository.SaveChangesAsync(token);

                return (
                    MoveTaskOutcome.Moved,
                    sourceColumnId == targetColumnId
                        ? [targetColumnId]
                        : new List<int> { sourceColumnId, targetColumnId });
            },
            cancellationToken);

        if (affectedColumnIds is null)
        {
            return (outcome, null);
        }

        // A move changes which column the task is counted in, which is exactly what the summary's
        // buckets are.
        await summaryCache.RemoveAsync(targetColumn.BoardId, cancellationToken);

        await NotifyBoardChangedAsync(targetColumn.BoardId, "taskMoved", cancellationToken);

        // Two ids mean the task genuinely left one column for another; a reorder returns one and is
        // news to nobody. Of the destinations, only the two system columns say something happened to
        // the work rather than to the board.
        if (affectedColumnIds.Count == 2)
        {
            var type = targetColumn.SystemKind switch
            {
                SystemColumnKind.Completed => NotificationType.TaskCompleted,
                SystemColumnKind.Cancelled => NotificationType.TaskCancelled,

                // To Do, and every custom column: a task moving through the board is its ordinary
                // progress, and announcing each step would be noise.
                _ => (NotificationType?)null
            };

            if (type is NotificationType announced)
            {
                await PublishNotificationAsync(
                    announced,
                    taskId,
                    actorUserId,
                    await TeamExceptActorAsync(targetColumn.BoardId, actorUserId, cancellationToken),
                    cancellationToken);
            }
        }

        // Read back outside the lock, through the same sprint the caller was looking at, so the
        // columns that come back are the ones they can see.
        return (outcome, await ReadColumnsAsync(affectedColumnIds, request.SprintId, cancellationToken));
    }

    // --- The task card ------------------------------------------------------
    //
    // None of what follows invalidates the board summary. It counts tasks by their column and by
    // whether they carry a sprint; a comment, an attachment and a priority appear in none of those.

    /// <summary>The whole card in one read. <c>null</c> when there is no such task.</summary>
    public async Task<TaskDetailResponseDto?> GetDetailAsync(int taskId, CancellationToken cancellationToken)
        => (await taskRepository.GetDetailAsync(taskId, cancellationToken))?.ToDetailResponseDto();

    /// <summary>
    /// Adds a comment. The author comes from the caller's token — the request carries no user id,
    /// so there is nothing to spoof. <c>null</c> when there is no such task.
    /// </summary>
    public async Task<TaskCommentResponseDto?> AddCommentAsync(
        int taskId,
        CreateTaskCommentRequestDto request,
        int userId,
        CancellationToken cancellationToken)
    {
        // The board itself, not just its existence: the team told about this comment is its own.
        var boardId = await taskRepository.GetBoardIdAsync(taskId, cancellationToken);

        if (boardId is null)
        {
            return null;
        }

        // A comment that is only whitespace never reaches here — [Required] rejects it.
        var comment = new TaskComment
        {
            TaskId = taskId,
            UserId = userId,
            Content = request.Content.Trim()

            // CreatedDate is intentionally not set; the database default supplies it.
        };

        taskRepository.AddComment(comment);

        await taskRepository.SaveChangesAsync(cancellationToken);

        // To the whole team rather than the assignee alone; the author is left out below.
        await PublishNotificationAsync(
            NotificationType.TaskCommented,
            taskId,
            userId,
            await TeamExceptActorAsync(boardId.Value, userId, cancellationToken),
            cancellationToken);

        var saved = await taskRepository.GetCommentAsync(comment.TaskCommentId, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Comment {comment.TaskCommentId} was saved but could not be read back.");

        return saved.ToResponseDto();
    }

    /// <summary>
    /// Removes a comment, and the files posted with it.
    /// </summary>
    /// <remarks>
    /// The author check is the one place in this project where being the creator of something
    /// decides what may be done to it. It is deliberately a single condition here rather than a
    /// policy: everywhere else, ownership still grants nothing.
    /// </remarks>
    public async Task<DeleteCommentOutcome> DeleteCommentAsync(
        int taskId,
        int commentId,
        int callerUserId,
        CancellationToken cancellationToken)
    {
        var comment = await taskRepository.GetCommentForUpdateAsync(commentId, cancellationToken);

        // A comment id from another task is not found, so this leaks nothing about other ids.
        if (comment is null || comment.TaskId != taskId)
        {
            return DeleteCommentOutcome.NotFound;
        }

        if (comment.UserId != callerUserId)
        {
            return DeleteCommentOutcome.NotAuthor;
        }

        // The FK is Restrict, so these do not go on their own — and their files never would.
        var attachments = await taskRepository.GetCommentAttachmentsForUpdateAsync(
            commentId,
            cancellationToken);

        taskRepository.RemoveRange(attachments);
        taskRepository.Remove(comment);

        await taskRepository.SaveChangesAsync(cancellationToken);

        // After the commit: a deleted file cannot be put back if the save then fails.
        foreach (var attachment in attachments)
        {
            attachmentStorage.Delete(attachment.StoredName);
        }

        return DeleteCommentOutcome.Deleted;
    }

    /// <summary>
    /// Stores an uploaded file and records what it was. The bytes go to the attachment folder under
    /// a name the server generates; the row keeps the name the uploader's file had.
    /// </summary>
    /// <param name="fileName">
    /// What the client called it. Used for the download filename and shown to people — never as a
    /// path, and only its extension decides anything.
    /// </param>
    /// <param name="commentId">
    /// The comment this file was posted with, when it was posted with one. It has to belong to the
    /// same task, or the thread could show a file under a message on a different card.
    /// </param>
    public async Task<(AddAttachmentOutcome Outcome, TaskAttachmentResponseDto? Attachment)> AddAttachmentAsync(
        int taskId,
        string fileName,
        long sizeBytes,
        Stream content,
        int uploadedByUserId,
        int? commentId,
        CancellationToken cancellationToken)
    {
        // Any directory part the client sent is dropped here, before anything looks at the name.
        var leafName = Path.GetFileName(fileName);

        if (string.IsNullOrWhiteSpace(leafName) || sizeBytes <= 0)
        {
            return (AddAttachmentOutcome.FileMissing, null);
        }

        if (sizeBytes > AttachmentStorage.MaxSizeBytes)
        {
            return (AddAttachmentOutcome.FileTooLarge, null);
        }

        // The content type comes from the allowlist, not from the upload's own header.
        var contentType = attachmentStorage.ContentTypeFor(Path.GetExtension(leafName));

        if (contentType is null)
        {
            return (AddAttachmentOutcome.FileTypeNotAllowed, null);
        }

        if (!await taskRepository.ExistsAsync(taskId, cancellationToken))
        {
            return (AddAttachmentOutcome.TaskNotFound, null);
        }

        // A comment on another task is treated as not found, as the delete does.
        if (commentId is int comment
            && !await taskRepository.CommentExistsOnTaskAsync(comment, taskId, cancellationToken))
        {
            return (AddAttachmentOutcome.CommentNotFound, null);
        }

        var storedName = await attachmentStorage.SaveAsync(
            content,
            Path.GetExtension(leafName),
            cancellationToken);

        var attachment = new TaskAttachment
        {
            TaskId = taskId,
            TaskCommentId = commentId,
            FileName = Truncate(leafName, 255),
            StoredName = storedName,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            UploadedByUserId = uploadedByUserId
        };

        taskRepository.AddAttachment(attachment);

        try
        {
            await taskRepository.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // The file is on disk and the row that gives it meaning is not; remove it.
            attachmentStorage.Delete(storedName);

            throw;
        }

        var saved = await taskRepository.GetAttachmentAsync(attachment.TaskAttachmentId, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Attachment {attachment.TaskAttachmentId} was saved but could not be read back.");

        return (AddAttachmentOutcome.Added, saved.ToResponseDto());
    }

    /// <summary>
    /// The file behind an attachment, with what is needed to name it in the response. <c>null</c>
    /// when the attachment does not exist, belongs to another task, or its file has gone.
    /// </summary>
    public async Task<(TaskAttachmentResponseDto Metadata, Stream Content)?> OpenAttachmentAsync(
        int taskId,
        int attachmentId,
        CancellationToken cancellationToken)
    {
        var attachment = await taskRepository.GetAttachmentAsync(attachmentId, cancellationToken);

        // Addressed through its task, so an id that belongs to a different one is simply not found.
        if (attachment is null || attachment.TaskId != taskId)
        {
            return null;
        }

        var content = attachmentStorage.Open(attachment.StoredName);

        return content is null ? null : (attachment.ToResponseDto(), content);
    }

    public async Task<bool> DeleteAttachmentAsync(
        int taskId,
        int attachmentId,
        CancellationToken cancellationToken)
    {
        var attachment = await taskRepository.GetAttachmentForUpdateAsync(attachmentId, cancellationToken);

        if (attachment is null || attachment.TaskId != taskId)
        {
            return false;
        }

        taskRepository.Remove(attachment);

        await taskRepository.SaveChangesAsync(cancellationToken);

        // After the commit: a deleted file cannot be put back if the save then fails.
        attachmentStorage.Delete(attachment.StoredName);

        return true;
    }

    /// <summary>Keeps a stored name inside its column, whatever the client called the file.</summary>
    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];

    /// <summary>
    /// Which column a new task starts in: To Do when the caller said nothing, otherwise the column
    /// they named — provided it belongs to this board.
    /// </summary>
    private async Task<(CreateTaskOutcome Outcome, WorkflowColumn? Column)> ResolveStartingColumnAsync(
        int boardId,
        int? requestedColumnId,
        CancellationToken cancellationToken)
    {
        if (requestedColumnId is null)
        {
            var toDo = await boardRepository.GetSystemColumnAsync(
                boardId,
                SystemColumnKind.ToDo,
                cancellationToken);

            // Every board is created with a To Do column and it cannot be deleted, so its absence
            // means the data is broken, not the request.
            return (
                CreateTaskOutcome.Created,
                toDo ?? throw new InvalidOperationException(
                    $"Board {boardId} is missing its To Do column, which every board is required to have."));
        }

        var column = await boardRepository.GetColumnAsync(requestedColumnId.Value, cancellationToken);

        if (column is null)
        {
            return (CreateTaskOutcome.ColumnNotFound, null);
        }

        // A task must never end up in another board's column; refusing here keeps the composite
        // foreign key from being the thing that reports it.
        return column.BoardId != boardId
            ? (CreateTaskOutcome.ColumnOnDifferentBoard, null)
            : (CreateTaskOutcome.Created, column);
    }

    /// <summary>
    /// Turns a slot counted among the cards the caller could see into a slot in the whole column.
    /// </summary>
    /// <remarks>
    /// Positions run across every task in a column, but a board scoped to a sprint shows only that
    /// sprint's. "Drop it third" therefore means third among the visible cards, which is somewhere
    /// else entirely once the hidden ones are counted back in — without this, a drag on a scoped
    /// board would silently move cards past tasks the user could not see.
    /// <para>
    /// The moved task has already been taken out of <paramref name="columnTasks"/> by the caller,
    /// so every index here refers to the column without it.
    /// </para>
    /// </remarks>
    /// <param name="columnTasks">The target column's tasks in order, excluding the one being moved.</param>
    /// <param name="visibleIndex">The slot the caller asked for, counted among what they could see.</param>
    /// <param name="sprintId">
    /// What they could see. <c>null</c> means the whole column, so the index needs no translating.
    /// </param>
    private static int ToColumnIndex(List<TaskItem> columnTasks, int visibleIndex, int? sprintId)
    {
        // Clamped rather than rejected: dropped past the last card means it goes last.
        if (sprintId is null)
        {
            return Math.Clamp(visibleIndex, 0, columnTasks.Count);
        }

        var visible = columnTasks.FindAll(task => task.SprintId == sprintId);

        // No visible card to sit before or after, so the end of the column, which leaves the tasks
        // already there undisturbed.
        if (visible.Count == 0)
        {
            return columnTasks.Count;
        }

        var clamped = Math.Clamp(visibleIndex, 0, visible.Count);

        // Immediately after the last visible card — not the end of the column, which would jump
        // the task over any hidden cards that follow.
        return clamped == visible.Count
            ? columnTasks.IndexOf(visible[^1]) + 1
            : columnTasks.IndexOf(visible[clamped]);
    }

    /// <summary>
    /// Whether a sprint may be used on this board: it must exist and belong to it. <c>null</c> in
    /// means no sprint was asked for, and <c>null</c> out means there is nothing wrong.
    /// </summary>
    private async Task<CreateTaskOutcome?> CheckSprintAsync(
        int? sprintId,
        int boardId,
        CancellationToken cancellationToken)
    {
        if (sprintId is not int requested)
        {
            return null;
        }

        var sprint = await sprintRepository.GetByIdAsync(requested, cancellationToken);

        if (sprint is null)
        {
            return CreateTaskOutcome.SprintNotFound;
        }

        return sprint.BoardId == boardId ? null : CreateTaskOutcome.SprintOnDifferentBoard;
    }

    /// <summary>
    /// Rewrites positions as 0, 1, 2 … over the list as it now stands. Called on every list a
    /// change touched, in the same save, so positions are contiguous by construction rather than
    /// by arithmetic done at each call site.
    /// </summary>
    private static void Renumber(List<TaskItem> tasks)
    {
        for (var index = 0; index < tasks.Count; index++)
        {
            tasks[index].Position = index;
        }
    }

    /// <summary>Blank and whitespace-only descriptions are stored as nothing at all.</summary>
    private static string? Normalise(string? description)
        => string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    /// <summary>
    /// Re-reads the task so the response carries the creator's name and the date the database
    /// generated, rather than anything assembled here.
    /// </summary>
    private async Task<TaskResponseDto> ReadBackAsync(int taskId, CancellationToken cancellationToken)
    {
        var saved = await taskRepository.GetWithDetailsAsync(taskId, cancellationToken)
            ?? throw new InvalidOperationException($"Task {taskId} was saved but could not be read back.");

        return saved.ToResponseDto();
    }

    /// <summary>
    /// The columns a move touched, as the caller sees them.
    /// </summary>
    /// <param name="sprintId">
    /// The sprint the caller was looking at, or <c>null</c> for the whole board. The columns are
    /// filtered to match: a client on a sprint-scoped board replaces its copy of these columns with
    /// what comes back, so returning every task would put tasks from other sprints on screen.
    /// </param>
    private async Task<IReadOnlyList<WorkflowColumnResponseDto>> ReadColumnsAsync(
        IReadOnlyCollection<int> columnIds,
        int? sprintId,
        CancellationToken cancellationToken)
    {
        var tasks = await taskRepository.GetTasksWithDetailsAsync(columnIds, cancellationToken);

        // Filtered here rather than in the query: at most two columns, against a second read.
        var tasksByColumn = tasks
            .Where(task => sprintId is null || task.SprintId == sprintId)
            .ToLookup(task => task.TaskColumnId);

        var columns = new List<WorkflowColumnResponseDto>();

        foreach (var columnId in columnIds)
        {
            var column = await boardRepository.GetColumnAsync(columnId, cancellationToken)
                ?? throw new InvalidOperationException($"Column {columnId} disappeared while moving a task.");

            columns.Add(column.ToResponseDto(tasksByColumn[columnId]));
        }

        return columns;
    }
}
