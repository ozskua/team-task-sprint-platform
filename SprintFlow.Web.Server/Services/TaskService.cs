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

    private async Task<IReadOnlyList<int>> TeamExceptActorAsync(
        int boardId,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var memberIds = await taskRepository.GetBoardMemberIdsAsync(boardId, cancellationToken);

        return memberIds.Where(memberId => memberId != actorUserId).ToList();
    }

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

        var (outcome, taskId) = await boardRepository.RunBoardOrderingAsync(
            boardId,
            async token =>
            {
                var (resolution, column) = await ResolveStartingColumnAsync(boardId, request.ColumnId, token);

                if (column is null)
                {
                    return (resolution, (int?)null);
                }

                if (!await IsAssignableAsync(request.AssigneeUserId, token))
                {
                    return (CreateTaskOutcome.AssigneeUnavailable, null);
                }

                var sprintCheck = await CheckSprintAsync(request.SprintId, boardId, token);

                if (sprintCheck is CreateTaskOutcome problem)
                {
                    return (problem, null);
                }

                var position = await taskRepository.CountInColumnAsync(column.WorkflowColumnId, token);

                var task = new TaskItem
                {
                    BoardId = boardId,
                    TaskColumnId = column.WorkflowColumnId,
                    Position = position,

                    Title = request.Title.Trim(),
                    Description = Normalise(request.Description),
                    Priority = request.Priority,

                    CreatedByUserId = createdByUserId,

                    SprintId = request.SprintId
                };

                taskRepository.Add(task);

                if (request.AssigneeUserId is int assignee)
                {
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

        if (request.AssigneeUserId is int assignee && assignee != createdByUserId)
        {
            await PublishNotificationAsync(
                NotificationType.TaskAssigned,
                taskId.Value,
                createdByUserId,
                [assignee],
                cancellationToken);
        }

        return (outcome, await ReadBackAsync(taskId.Value, cancellationToken));
    }

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

                var changed = await ReplaceAssigneeAsync(taskId, request.AssigneeUserId, token);

                await taskRepository.SaveChangesAsync(token);

                return (UpdateTaskOutcome.Updated, changed);
            },
            cancellationToken);

        if (outcome != UpdateTaskOutcome.Updated)
        {
            return (outcome, null);
        }

        await NotifyBoardChangedAsync(boardId.Value, "taskUpdated", cancellationToken);

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

    private async Task<bool> IsAssignableAsync(int? assigneeUserId, CancellationToken cancellationToken)
        => assigneeUserId is null
           || await userRepository.IsActiveAsync(assigneeUserId.Value, cancellationToken);

    private async Task<bool> ReplaceAssigneeAsync(
        int taskId,
        int? assigneeUserId,
        CancellationToken cancellationToken)
    {
        var existing = await taskRepository.GetParticipantsAsync(taskId, cancellationToken);

        if (assigneeUserId is int wanted && existing.Count == 1 && existing[0].UserId == wanted)
        {
            return false;
        }

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

            if (sprint.BoardId != task.BoardId)
            {
                return (AssignSprintOutcome.SprintOnDifferentBoard, null);
            }
        }

        task.SprintId = request.SprintId;

        await taskRepository.SaveChangesAsync(cancellationToken);

        await summaryCache.RemoveAsync(task.BoardId, cancellationToken);

        await NotifyBoardChangedAsync(task.BoardId, "taskSprintChanged", cancellationToken);

        return (AssignSprintOutcome.Assigned, await ReadBackAsync(taskId, cancellationToken));
    }

    public async Task<bool> DeleteAsync(int taskId, CancellationToken cancellationToken)
    {
        var boardId = await taskRepository.GetBoardIdAsync(taskId, cancellationToken);

        if (boardId is null)
        {
            return false;
        }

        var attachmentNames = await taskRepository.GetAttachmentNamesAsync(taskId, cancellationToken);

        var deleted = await boardRepository.RunBoardOrderingAsync(
            boardId.Value,
            async token =>
            {
                var task = await taskRepository.GetForUpdateAsync(taskId, token);

                if (task is null)
                {
                    return false;
                }

                var columnTasks = await taskRepository.GetColumnTasksForUpdateAsync(task.TaskColumnId, token);

                taskRepository.Remove(task);
                columnTasks.RemoveAll(candidate => candidate.TaskId == taskId);
                Renumber(columnTasks);

                await taskRepository.SaveChangesAsync(token);

                return true;
            },
            cancellationToken);

        if (deleted)
        {
            foreach (var storedName in attachmentNames)
            {
                attachmentStorage.Delete(storedName);
            }

            await summaryCache.RemoveAsync(boardId.Value, cancellationToken);

            await NotifyBoardChangedAsync(boardId.Value, "taskDeleted", cancellationToken);
        }

        return deleted;
    }

    public async Task<(MoveTaskOutcome Outcome, IReadOnlyList<WorkflowColumnResponseDto>? Columns)> MoveAsync(
        int taskId,
        MoveTaskRequestDto request,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var targetColumn = await boardRepository.GetColumnAsync(request.TargetColumnId, cancellationToken);

        if (targetColumn is null)
        {
            return (MoveTaskOutcome.ColumnNotFound, null);
        }

        var (outcome, affectedColumnIds) = await boardRepository.RunBoardOrderingAsync(
            targetColumn.BoardId,
            async token =>
            {
                var task = await taskRepository.GetForUpdateAsync(taskId, token);

                if (task is null)
                {
                    return (MoveTaskOutcome.TaskNotFound, (List<int>?)null);
                }

                if (targetColumn.BoardId != task.BoardId)
                {
                    return (MoveTaskOutcome.ColumnOnDifferentBoard, null);
                }

                var sourceColumnId = task.TaskColumnId;
                var targetColumnId = targetColumn.WorkflowColumnId;

                var sourceTasks = await taskRepository.GetColumnTasksForUpdateAsync(sourceColumnId, token);
                sourceTasks.RemoveAll(candidate => candidate.TaskId == taskId);

                var targetTasks = sourceColumnId == targetColumnId
                    ? sourceTasks
                    : await taskRepository.GetColumnTasksForUpdateAsync(targetColumnId, token);

                var index = ToColumnIndex(targetTasks, request.TargetIndex, request.SprintId);

                task.TaskColumnId = targetColumnId;
                targetTasks.Insert(index, task);

                Renumber(sourceTasks);

                if (!ReferenceEquals(sourceTasks, targetTasks))
                {
                    Renumber(targetTasks);
                }

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

        await summaryCache.RemoveAsync(targetColumn.BoardId, cancellationToken);

        await NotifyBoardChangedAsync(targetColumn.BoardId, "taskMoved", cancellationToken);

        if (affectedColumnIds.Count == 2)
        {
            var type = targetColumn.SystemKind switch
            {
                SystemColumnKind.Completed => NotificationType.TaskCompleted,
                SystemColumnKind.Cancelled => NotificationType.TaskCancelled,

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

        return (outcome, await ReadColumnsAsync(affectedColumnIds, request.SprintId, cancellationToken));
    }

public async Task<TaskDetailResponseDto?> GetDetailAsync(int taskId, CancellationToken cancellationToken)
        => (await taskRepository.GetDetailAsync(taskId, cancellationToken))?.ToDetailResponseDto();

    public async Task<TaskCommentResponseDto?> AddCommentAsync(
        int taskId,
        CreateTaskCommentRequestDto request,
        int userId,
        CancellationToken cancellationToken)
    {
        var boardId = await taskRepository.GetBoardIdAsync(taskId, cancellationToken);

        if (boardId is null)
        {
            return null;
        }

        var comment = new TaskComment
        {
            TaskId = taskId,
            UserId = userId,
            Content = request.Content.Trim()
        };

        taskRepository.AddComment(comment);

        await taskRepository.SaveChangesAsync(cancellationToken);

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

    public async Task<DeleteCommentOutcome> DeleteCommentAsync(
        int taskId,
        int commentId,
        int callerUserId,
        CancellationToken cancellationToken)
    {
        var comment = await taskRepository.GetCommentForUpdateAsync(commentId, cancellationToken);

        if (comment is null || comment.TaskId != taskId)
        {
            return DeleteCommentOutcome.NotFound;
        }

        if (comment.UserId != callerUserId)
        {
            return DeleteCommentOutcome.NotAuthor;
        }

        var attachments = await taskRepository.GetCommentAttachmentsForUpdateAsync(
            commentId,
            cancellationToken);

        taskRepository.RemoveRange(attachments);
        taskRepository.Remove(comment);

        await taskRepository.SaveChangesAsync(cancellationToken);

        foreach (var attachment in attachments)
        {
            attachmentStorage.Delete(attachment.StoredName);
        }

        return DeleteCommentOutcome.Deleted;
    }

    public async Task<(AddAttachmentOutcome Outcome, TaskAttachmentResponseDto? Attachment)> AddAttachmentAsync(
        int taskId,
        string fileName,
        long sizeBytes,
        Stream content,
        int uploadedByUserId,
        int? commentId,
        CancellationToken cancellationToken)
    {
        var leafName = Path.GetFileName(fileName);

        if (string.IsNullOrWhiteSpace(leafName) || sizeBytes <= 0)
        {
            return (AddAttachmentOutcome.FileMissing, null);
        }

        if (sizeBytes > AttachmentStorage.MaxSizeBytes)
        {
            return (AddAttachmentOutcome.FileTooLarge, null);
        }

        var contentType = attachmentStorage.ContentTypeFor(Path.GetExtension(leafName));

        if (contentType is null)
        {
            return (AddAttachmentOutcome.FileTypeNotAllowed, null);
        }

        if (!await taskRepository.ExistsAsync(taskId, cancellationToken))
        {
            return (AddAttachmentOutcome.TaskNotFound, null);
        }

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
            attachmentStorage.Delete(storedName);

            throw;
        }

        var saved = await taskRepository.GetAttachmentAsync(attachment.TaskAttachmentId, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Attachment {attachment.TaskAttachmentId} was saved but could not be read back.");

        return (AddAttachmentOutcome.Added, saved.ToResponseDto());
    }

    public async Task<(TaskAttachmentResponseDto Metadata, Stream Content)?> OpenAttachmentAsync(
        int taskId,
        int attachmentId,
        CancellationToken cancellationToken)
    {
        var attachment = await taskRepository.GetAttachmentAsync(attachmentId, cancellationToken);

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

        attachmentStorage.Delete(attachment.StoredName);

        return true;
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];

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

        return column.BoardId != boardId
            ? (CreateTaskOutcome.ColumnOnDifferentBoard, null)
            : (CreateTaskOutcome.Created, column);
    }

    private static int ToColumnIndex(List<TaskItem> columnTasks, int visibleIndex, int? sprintId)
    {
        if (sprintId is null)
        {
            return Math.Clamp(visibleIndex, 0, columnTasks.Count);
        }

        var visible = columnTasks.FindAll(task => task.SprintId == sprintId);

        if (visible.Count == 0)
        {
            return columnTasks.Count;
        }

        var clamped = Math.Clamp(visibleIndex, 0, visible.Count);

        return clamped == visible.Count
            ? columnTasks.IndexOf(visible[^1]) + 1
            : columnTasks.IndexOf(visible[clamped]);
    }

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

    private static void Renumber(List<TaskItem> tasks)
    {
        for (var index = 0; index < tasks.Count; index++)
        {
            tasks[index].Position = index;
        }
    }

    private static string? Normalise(string? description)
        => string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    private async Task<TaskResponseDto> ReadBackAsync(int taskId, CancellationToken cancellationToken)
    {
        var saved = await taskRepository.GetWithDetailsAsync(taskId, cancellationToken)
            ?? throw new InvalidOperationException($"Task {taskId} was saved but could not be read back.");

        return saved.ToResponseDto();
    }

    private async Task<IReadOnlyList<WorkflowColumnResponseDto>> ReadColumnsAsync(
        IReadOnlyCollection<int> columnIds,
        int? sprintId,
        CancellationToken cancellationToken)
    {
        var tasks = await taskRepository.GetTasksWithDetailsAsync(columnIds, cancellationToken);

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
