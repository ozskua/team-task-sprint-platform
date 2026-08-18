using SprintFlow.Web.Server.Common.Enums;
using SprintFlow.Web.Server.DTOs.Columns;
using SprintFlow.Web.Server.DTOs.Tasks;
using SprintFlow.Web.Server.Interfaces.Repositories;
using SprintFlow.Web.Server.Interfaces.Services;
using SprintFlow.Web.Server.Mappings;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Services;

public class TaskService : ITaskService
{
    private readonly ITaskRepository taskRepository;
    private readonly IWorkflowColumnRepository columnRepository;
    private readonly IBoardRepository boardRepository;
    private readonly ITaskParticipantRepository participantRepository;
    private readonly IUserRepository userRepository;

    public TaskService(
        ITaskRepository taskRepository,
        IWorkflowColumnRepository columnRepository,
        IBoardRepository boardRepository,
        ITaskParticipantRepository participantRepository,
        IUserRepository userRepository)
    {
        this.taskRepository = taskRepository;
        this.columnRepository = columnRepository;
        this.boardRepository = boardRepository;
        this.participantRepository = participantRepository;
        this.userRepository = userRepository;
    }

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

                    // From the caller's token. The request DTO has no property for it, so nothing a
                    // client sends can influence this.
                    CreatedByUserId = createdByUserId

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

        // Read back outside the lock: the row is committed by now, and holding the board while
        // assembling a response would make every other request on it wait for no reason.
        return taskId is null
            ? (outcome, null)
            : (outcome, await ReadBackAsync(taskId.Value, cancellationToken));
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

                await ReplaceAssigneeAsync(taskId, request.AssigneeUserId, token);

                // The creator, the column and the position are deliberately untouched: editing what
                // a task says is not the same operation as moving it, and ownership is never
                // editable. One save covers the text and the assignment together.
                await taskRepository.SaveChangesAsync(token);

                return UpdateTaskOutcome.Updated;
            },
            cancellationToken);

        return outcome == UpdateTaskOutcome.Updated
            ? (outcome, await ReadBackAsync(taskId, cancellationToken))
            : (outcome, null);
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
        var existing = await participantRepository.GetForTaskAsync(taskId, cancellationToken);

        // Already the right person: leave the row alone so its AddedDate is not reset by an edit
        // that did not change the assignment.
        if (assigneeUserId is int wanted && existing.Count == 1 && existing[0].UserId == wanted)
        {
            return;
        }

        if (existing.Count > 0)
        {
            participantRepository.RemoveRange(existing);
        }

        if (assigneeUserId is int assignee)
        {
            participantRepository.Add(new TaskParticipant { TaskId = taskId, UserId = assignee });
        }
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

        // Under the board's lock: deleting renumbers the survivors, which is the same kind of
        // rewrite a move performs and has to be serialised for the same reason.
        return await boardRepository.RunBoardOrderingAsync(
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
    }

    public async Task<(MoveTaskOutcome Outcome, IReadOnlyList<WorkflowColumnResponseDto>? Columns)> MoveAsync(
        int taskId,
        MoveTaskRequestDto request,
        CancellationToken cancellationToken)
    {
        // Read without the lock only to learn which board to lock. The target column names the
        // board the move must happen on; if the task turns out to belong to a different one, the
        // move is rejected inside and nothing is written.
        var targetColumn = await columnRepository.GetByIdAsync(request.TargetColumnId, cancellationToken);

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

                // Clamped rather than rejected: a card dropped past the last one plainly means "put
                // it last", and a client should not have to know how many cards a column holds.
                var index = Math.Clamp(request.TargetIndex, 0, targetTasks.Count);

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

        // Read back outside the lock: the move is committed, and assembling a response is not
        // ordering work.
        return affectedColumnIds is null
            ? (outcome, null)
            : (outcome, await ReadColumnsAsync(affectedColumnIds, cancellationToken));
    }

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
            var toDo = await columnRepository.GetSystemColumnAsync(
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

        var column = await columnRepository.GetByIdAsync(requestedColumnId.Value, cancellationToken);

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

    private async Task<IReadOnlyList<WorkflowColumnResponseDto>> ReadColumnsAsync(
        IReadOnlyCollection<int> columnIds,
        CancellationToken cancellationToken)
    {
        var tasks = await taskRepository.GetTasksWithDetailsAsync(columnIds, cancellationToken);
        var tasksByColumn = tasks.ToLookup(task => task.TaskColumnId);

        var columns = new List<WorkflowColumnResponseDto>();

        foreach (var columnId in columnIds)
        {
            var column = await columnRepository.GetByIdAsync(columnId, cancellationToken)
                ?? throw new InvalidOperationException($"Column {columnId} disappeared while moving a task.");

            columns.Add(column.ToResponseDto(tasksByColumn[columnId]));
        }

        return columns;
    }
}
