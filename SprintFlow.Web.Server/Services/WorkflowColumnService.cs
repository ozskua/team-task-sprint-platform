using SprintFlow.Web.Server.Common.Enums;
using SprintFlow.Web.Server.Data.Repositories;
using SprintFlow.Web.Server.DTOs.Columns;
using SprintFlow.Web.Server.Mappings;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Services;

public class WorkflowColumnService
{
    private readonly BoardRepository boardRepository;
    private readonly TaskRepository taskRepository;

    public WorkflowColumnService(
        BoardRepository boardRepository,
        TaskRepository taskRepository)
    {
        this.boardRepository = boardRepository;
        this.taskRepository = taskRepository;
    }

    public async Task<WorkflowColumnResponseDto?> CreateAsync(
        int boardId,
        CreateWorkflowColumnRequestDto request,
        CancellationToken cancellationToken)
    {
        // The only check there is. Any signed-in user may add a column to any board.
        if (!await boardRepository.ExistsAsync(boardId, cancellationToken))
        {
            return null;
        }

        // Under the board's lock: the existing columns are read and renumbered here.
        return await boardRepository.RunBoardOrderingAsync<WorkflowColumnResponseDto?>(
            boardId,
            async token =>
            {
                var columns = await boardRepository.GetBoardColumnsForUpdateAsync(boardId, token);

                var column = new WorkflowColumn
                {
                    BoardId = boardId,

                    Name = request.Name.Trim(),

                    // Always custom: a system column cannot be created after the board exists.
                    SystemKind = null

                    // CreatedDate is intentionally not set; the database default supplies it.
                };

                boardRepository.AddColumn(column);

                // Appended to the custom columns, which is what "immediately before Completed"
                // means once the canonical order is applied.
                columns.Add(column);
                ApplyCanonicalOrder(columns);

                // One save for the insert and every changed position, so the board is never
                // briefly ordered in a way no rule allows.
                await boardRepository.SaveChangesAsync(token);

                return column.ToResponseDto();
            },
            cancellationToken);
    }

    public async Task<(RenameColumnOutcome Outcome, WorkflowColumnResponseDto? Column)> RenameAsync(
        int columnId,
        RenameWorkflowColumnRequestDto request,
        CancellationToken cancellationToken)
    {
        var column = await boardRepository.GetColumnForUpdateAsync(columnId, cancellationToken);

        if (column is null)
        {
            return (RenameColumnOutcome.ColumnNotFound, null);
        }

        if (column.SystemKind is not null)
        {
            return (RenameColumnOutcome.ColumnIsSystem, null);
        }

        column.Name = request.Name.Trim();

        await boardRepository.SaveChangesAsync(cancellationToken);

        // Read the tasks back: renaming changes nothing about them, and an empty list would imply
        // otherwise.
        var tasks = await taskRepository.GetTasksWithDetailsAsync([columnId], cancellationToken);

        return (RenameColumnOutcome.Renamed, column.ToResponseDto(tasks));
    }

    public async Task<DeleteColumnOutcome> DeleteAsync(int columnId, CancellationToken cancellationToken)
    {
        // Read without the lock only to learn which board to lock and to reject a system column
        // early. Everything this decision rests on is read again inside.
        var column = await boardRepository.GetColumnAsync(columnId, cancellationToken);

        if (column is null)
        {
            return DeleteColumnOutcome.ColumnNotFound;
        }

        if (column.SystemKind is not null)
        {
            return DeleteColumnOutcome.ColumnIsSystem;
        }

        return await boardRepository.RunBoardOrderingAsync(
            column.BoardId,
            async token =>
            {
                // Re-read under the lock: between the check above and here, another request may
                // have deleted this column or moved a task into it.
                var tracked = await boardRepository.GetColumnForUpdateAsync(columnId, token);

                if (tracked is null)
                {
                    return DeleteColumnOutcome.ColumnNotFound;
                }

                if (tracked.SystemKind is not null)
                {
                    return DeleteColumnOutcome.ColumnIsSystem;
                }

                // Refused while the column holds anything; the caller empties it first. The
                // foreign key would refuse the delete anyway — this check turns that into a clear
                // 400. Asked under the lock, so a task cannot arrive before the delete.
                if (await boardRepository.ColumnHasTasksAsync(columnId, token))
                {
                    return DeleteColumnOutcome.ColumnHasTasks;
                }

                var columns = await boardRepository.GetBoardColumnsForUpdateAsync(tracked.BoardId, token);

                boardRepository.RemoveColumn(tracked);
                columns.RemoveAll(candidate => candidate.WorkflowColumnId == columnId);
                ApplyCanonicalOrder(columns);

                await boardRepository.SaveChangesAsync(token);

                return DeleteColumnOutcome.Deleted;
            },
            cancellationToken);
    }

    public async Task<(ReorderColumnsOutcome Outcome, IReadOnlyList<WorkflowColumnResponseDto>? Columns)> ReorderAsync(
        int boardId,
        ReorderColumnsRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!await boardRepository.ExistsAsync(boardId, cancellationToken))
        {
            return (ReorderColumnsOutcome.BoardNotFound, null);
        }

        // Under the board's lock, so a column created or deleted a moment ago cannot let a
        // valid-looking request write a broken order.
        return await boardRepository.RunBoardOrderingAsync(
            boardId,
            async token =>
            {
                var columns = await boardRepository.GetBoardColumnsForUpdateAsync(boardId, token);
                var requestedIds = request.OrderedColumnIds;

                // Before the set comparison, so naming a system column reports the real reason.
                var systemIds = columns
                    .Where(column => column.SystemKind is not null)
                    .Select(column => column.WorkflowColumnId)
                    .ToHashSet();

                if (requestedIds.Any(systemIds.Contains))
                {
                    return (ReorderColumnsOutcome.ContainsSystemColumn, (IReadOnlyList<WorkflowColumnResponseDto>?)null);
                }

                var customIds = columns
                    .Where(column => column.SystemKind is null)
                    .Select(column => column.WorkflowColumnId)
                    .ToHashSet();

                // Exactly the board's custom columns: same count catches repeats and omissions,
                // SetEquals catches anything foreign — including a column belonging to another board.
                if (requestedIds.Count != customIds.Count || !requestedIds.ToHashSet().SetEquals(customIds))
                {
                    return (ReorderColumnsOutcome.ColumnSetMismatch, null);
                }

                ApplyCanonicalOrder(columns, requestedIds);

                await boardRepository.SaveChangesAsync(token);

                var tasks = await taskRepository.GetTasksWithDetailsAsync(
                    columns.Select(column => column.WorkflowColumnId).ToList(),
                    token);

                return (
                    ReorderColumnsOutcome.Reordered,
                    columns.ToOrderedResponseDtos(tasks.ToLookup(task => task.TaskColumnId)));
            },
            cancellationToken);
    }

    /// <summary>
    /// Writes every position on the board from one rule: To Do first, then the custom columns,
    /// then Completed and Cancelled.
    /// </summary>
    /// <remarks>
    /// Because the three system columns are pinned, the whole order can always be rebuilt rather
    /// than patched. That is why no operation has to work out which positions shifted — there is
    /// no partial state to get wrong, and the result is contiguous by construction.
    /// </remarks>
    /// <param name="columns">Every column of one board.</param>
    /// <param name="customOrder">
    /// The custom columns in the order they should appear. Omitted, they keep the order they were
    /// read in.
    /// </param>
    private static void ApplyCanonicalOrder(List<WorkflowColumn> columns, IReadOnlyList<int>? customOrder = null)
    {
        var toDo = RequireSystemColumn(columns, SystemColumnKind.ToDo);
        var completed = RequireSystemColumn(columns, SystemColumnKind.Completed);
        var cancelled = RequireSystemColumn(columns, SystemColumnKind.Cancelled);

        var customs = columns.Where(column => column.SystemKind is null).ToList();

        if (customOrder is not null)
        {
            customs = customOrder
                .Select(id => customs.First(column => column.WorkflowColumnId == id))
                .ToList();
        }

        var position = 0;

        toDo.Position = position++;

        foreach (var custom in customs)
        {
            custom.Position = position++;
        }

        completed.Position = position++;
        cancelled.Position = position;
    }

    /// <summary>
    /// Every board is created with all three system columns and they can never be deleted, so a
    /// missing one means the data is broken rather than that the caller asked for something
    /// unreasonable. Saying so plainly beats silently producing a board with no To Do.
    /// </summary>
    private static WorkflowColumn RequireSystemColumn(List<WorkflowColumn> columns, SystemColumnKind kind)
        => columns.FirstOrDefault(column => column.SystemKind == kind)
           ?? throw new InvalidOperationException(
               $"Board is missing its {kind} column, which every board is required to have.");
}
