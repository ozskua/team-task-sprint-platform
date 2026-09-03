using Microsoft.AspNetCore.SignalR;
using SprintFlow.Web.Server.Common.Enums;
using SprintFlow.Web.Server.Data.Repositories;
using SprintFlow.Web.Server.DTOs.Columns;
using SprintFlow.Web.Server.DTOs.Tasks;
using SprintFlow.Web.Server.Hubs;
using SprintFlow.Web.Server.Mappings;
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
    private readonly IHubContext<BoardHub> boardHub;

    public TaskService(
        TaskRepository taskRepository,
        BoardRepository boardRepository,
        UserRepository userRepository,
        SprintRepository sprintRepository,
        IBoardSummaryCache summaryCache,
        AttachmentStorage attachmentStorage,
        IHubContext<BoardHub> boardHub)
    {
        this.taskRepository = taskRepository;
        this.boardRepository = boardRepository;
        this.userRepository = userRepository;
        this.sprintRepository = sprintRepository;
        this.summaryCache = summaryCache;
        this.attachmentStorage = attachmentStorage;
        this.boardHub = boardHub;
    }

    /// <summary>
    /// Tells the other clients watching this board to re-read it.
    /// </summary>
    /// <remarks>
    /// Separate from cache invalidation on purpose, and not always paired with it: the summary
    /// reports counts, while the board shows a task's title, priority and assignee. An edit changes
    /// what is on screen without changing a single figure the summary holds, so it publishes here
    /// and still, correctly, does not drop the cache entry.
    /// </remarks>
    private Task NotifyBoardChangedAsync(int boardId, string reason, CancellationToken cancellationToken)
        => boardHub.Clients
            .Group(BoardHub.GroupFor(boardId))
            .SendAsync("boardChanged", new { boardId, reason }, cancellationToken);

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

        // Under the board's lock, because the new task's position is derived from how many tasks
        // the column already holds. Two requests reading that count at the same time would both
        // pick the same index; the lock makes the second one wait and read the larger count.
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

                // Same reason, for the sprint. The composite foreign key would refuse a sprint from
                // another board anyway; checking here is what makes it a clear 400.
                var sprintCheck = await CheckSprintAsync(request.SprintId, boardId, token);

                if (sprintCheck is CreateTaskOutcome problem)
                {
                    return (problem, null);
                }

                // The next free slot. Positions are contiguous from zero, so the count is the index
                // a new task takes at the end of the column.
                var position = await taskRepository.CountInColumnAsync(column.WorkflowColumnId, token);

                var task = new TaskItem
                {
                    BoardId = boardId,
                    TaskColumnId = column.WorkflowColumnId,
                    Position = position,

                    // Stored without surrounding whitespace. A title that is only whitespace never
                    // reaches here — [Required] rejects it.
                    Title = request.Title.Trim(),
                    Description = Normalise(request.Description),
                    Priority = request.Priority,

                    // From the caller's token. The request DTO has no property for it, so nothing a
                    // client sends can influence this.
                    CreatedByUserId = createdByUserId,

                    // Normally the sprint the board is scoped to, so a task added while looking at
                    // a sprint belongs to it instead of seeming to disappear. Null when the caller
                    // was looking at the whole board.
                    SprintId = request.SprintId

                    // CreatedDate is intentionally not set; the database default supplies it.
                };

                taskRepository.Add(task);

                if (request.AssigneeUserId is int assignee)
                {
                    // Staged alongside the task itself, so one save writes both. A task is never
                    // briefly visible without the assignee it was created with.
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

        // The board has one more task than its cached summary says, in a column that summary counts.
        await summaryCache.RemoveAsync(boardId, cancellationToken);

        await NotifyBoardChangedAsync(boardId, "taskCreated", cancellationToken);

        // Read back outside the lock: the row is committed by now, and holding the board while
        // assembling a response would make every other request on it wait for no reason.
        return (outcome, await ReadBackAsync(taskId.Value, cancellationToken));
    }

    public async Task<(UpdateTaskOutcome Outcome, TaskResponseDto? Task)> UpdateAsync(
        int taskId,
        UpdateTaskRequestDto request,
        CancellationToken cancellationToken)
    {
        var boardId = await taskRepository.GetBoardIdAsync(taskId, cancellationToken);

        if (boardId is null)
        {
            return (UpdateTaskOutcome.TaskNotFound, null);
        }

        // Under the board's write lock. Replacing an assignee is a delete followed by an insert on
        // a uniquely-indexed column, so two edits arriving together must not interleave.
        var outcome = await boardRepository.RunBoardOrderingAsync(
            boardId.Value,
            async token =>
            {
                var task = await taskRepository.GetForUpdateAsync(taskId, token);

                if (task is null)
                {
                    return UpdateTaskOutcome.TaskNotFound;
                }

                if (!await IsAssignableAsync(request.AssigneeUserId, token))
                {
                    return UpdateTaskOutcome.AssigneeUnavailable;
                }

                task.Title = request.Title.Trim();
                task.Description = Normalise(request.Description);
                task.Priority = request.Priority;

                await ReplaceAssigneeAsync(taskId, request.AssigneeUserId, token);

                // The creator, the column and the position are deliberately untouched: editing what
                // a task says is not the same operation as moving it, and ownership is never
                // editable. One save covers the text and the assignment together.
                await taskRepository.SaveChangesAsync(token);

                return UpdateTaskOutcome.Updated;
            },
            cancellationToken);

        // The board summary is deliberately not invalidated here. This method changes a task's
        // title, description, priority and assignee, and the summary reports none of those — it
        // counts tasks by their column and by whether they carry a sprint, and this touches
        // neither. Dropping the entry would throw away a correct answer on every text edit.
        if (outcome != UpdateTaskOutcome.Updated)
        {
            return (outcome, null);
        }

        // The cards do show all four, though, so the other clients are told even though the cache
        // keeps its entry.
        await NotifyBoardChangedAsync(boardId.Value, "taskUpdated", cancellationToken);

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
    private async Task ReplaceAssigneeAsync(
        int taskId,
        int? assigneeUserId,
        CancellationToken cancellationToken)
    {
        var existing = await taskRepository.GetParticipantsAsync(taskId, cancellationToken);

        // Already the right person: leave the row alone so its AddedDate is not reset by an edit
        // that did not change the assignment.
        if (assigneeUserId is int wanted && existing.Count == 1 && existing[0].UserId == wanted)
        {
            return;
        }

        if (existing.Count > 0)
        {
            taskRepository.RemoveRange(existing);
        }

        if (assigneeUserId is int assignee)
        {
            taskRepository.Add(new TaskParticipant { TaskId = taskId, UserId = assignee });
        }
    }

    public async Task<(AssignSprintOutcome Outcome, TaskResponseDto? Task)> AssignSprintAsync(
        int taskId,
        AssignTaskSprintRequestDto request,
        CancellationToken cancellationToken)
    {
        // No board lock here, unlike the other writes. This changes one column on one row and
        // touches no position and no uniquely-indexed table, so there is nothing for two callers to
        // interleave: the later write simply wins, exactly as it does for a task's title.
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

            // The composite foreign key would refuse this too; checking first is what turns it into
            // a clear 400 rather than a constraint violation surfacing as a 500.
            if (sprint.BoardId != task.BoardId)
            {
                return (AssignSprintOutcome.SprintOnDifferentBoard, null);
            }
        }

        // Null takes the task out of whichever sprint it was in. The task keeps its column and its
        // position either way: which sprint a task belongs to and where it sits on the board are
        // different questions.
        task.SprintId = request.SprintId;

        await taskRepository.SaveChangesAsync(cancellationToken);

        // Changes how many of the board's tasks are in no sprint, which the summary reports.
        await summaryCache.RemoveAsync(task.BoardId, cancellationToken);

        await NotifyBoardChangedAsync(task.BoardId, "taskSprintChanged", cancellationToken);

        return (AssignSprintOutcome.Assigned, await ReadBackAsync(taskId, cancellationToken));
    }

    public async Task<bool> DeleteAsync(int taskId, CancellationToken cancellationToken)
    {
        // Read without the lock only to learn which board to lock. The decision is made again
        // inside, against rows that cannot change while it is held.
        var boardId = await taskRepository.GetBoardIdAsync(taskId, cancellationToken);

        if (boardId is null)
        {
            return false;
        }

        // Read before the delete, because the rows go by cascade and the files on disk do not.
        var attachmentNames = await taskRepository.GetAttachmentNamesAsync(taskId, cancellationToken);

        // Under the board's lock: deleting renumbers the survivors, which is the same kind of
        // rewrite a move performs and has to be serialised for the same reason.
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

                // The task's participation rows go with it by cascade, and team membership needs no
                // clean-up because it is derived from those rows rather than stored.
                await taskRepository.SaveChangesAsync(token);

                return true;
            },
            cancellationToken);

        if (deleted)
        {
            // The comment and attachment rows went with the task by cascade. The files did not, so
            // they are removed here — after the commit, because a deleted file cannot be rolled back.
            foreach (var storedName in attachmentNames)
            {
                attachmentStorage.Delete(storedName);
            }

            // One fewer task on the board, and one fewer in whichever bucket it was counted in.
            await summaryCache.RemoveAsync(boardId.Value, cancellationToken);

            await NotifyBoardChangedAsync(boardId.Value, "taskDeleted", cancellationToken);
        }

        return deleted;
    }

    public async Task<(MoveTaskOutcome Outcome, IReadOnlyList<WorkflowColumnResponseDto>? Columns)> MoveAsync(
        int taskId,
        MoveTaskRequestDto request,
        CancellationToken cancellationToken)
    {
        // Read without the lock only to learn which board to lock. The target column names the
        // board the move must happen on; if the task turns out to belong to a different one, the
        // move is rejected inside and nothing is written.
        var targetColumn = await boardRepository.GetColumnAsync(request.TargetColumnId, cancellationToken);

        if (targetColumn is null)
        {
            return (MoveTaskOutcome.ColumnNotFound, null);
        }

        // Under the board's lock. Both columns a move touches belong to the same board, so this one
        // lock covers both — there is never a second lock to acquire, and therefore no lock order
        // to get wrong and no deadlock to avoid.
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

                // The index the caller sent was counted among the cards they could see, which on a
                // sprint-scoped board is only some of the column. Translated here into a position
                // in the whole column, and clamped on the way.
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

        // A move changes which column the task is counted in, and the summary's buckets — and the
        // progress figure derived from them — are exactly that count. The board is the target
        // column's, which the check inside the lock has already proven is also the task's.
        await summaryCache.RemoveAsync(targetColumn.BoardId, cancellationToken);

        await NotifyBoardChangedAsync(targetColumn.BoardId, "taskMoved", cancellationToken);

        // Read back outside the lock: the move is committed, and assembling a response is not
        // ordering work. Reported through the same sprint the caller was looking at, so the columns
        // that come back are the columns they can see rather than the whole board's.
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
        if (!await taskRepository.ExistsAsync(taskId, cancellationToken))
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

        // A comment id belonging to another task is simply not found, so this cannot be used to
        // learn which ids exist elsewhere.
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

        // A comment on another task is treated as no comment at all being found, for the same
        // reason the delete does: the id says nothing about what exists elsewhere.
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
            // The file is already on disk and the row that gives it meaning is not. Removing it
            // keeps the folder free of files nothing points at.
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

            // Every board is created with a To Do column and it can never be deleted, so its
            // absence means the data is broken, not that the request was wrong.
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

        // Nothing is created. A task must never end up in another board's column, and refusing here
        // means the composite foreign key never has to be the thing that reports it.
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
        // Clamped rather than rejected: a card dropped past the last one plainly means "put it
        // last", and a client should not have to know how many cards a column holds.
        if (sprintId is null)
        {
            return Math.Clamp(visibleIndex, 0, columnTasks.Count);
        }

        var visible = columnTasks.FindAll(task => task.SprintId == sprintId);

        // Nothing of this sprint is in the column, so there is no visible card to sit before or
        // after. The end of the column is as good a place as any, and it leaves the tasks already
        // there undisturbed.
        if (visible.Count == 0)
        {
            return columnTasks.Count;
        }

        var clamped = Math.Clamp(visibleIndex, 0, visible.Count);

        // Past the last visible card means immediately after it — not the end of the column, which
        // would jump the task over any hidden cards that follow.
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

        // Filtered here rather than in the query: this is at most two columns, and the alternative
        // is a second repository read that differs from the first only by a where clause.
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
