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
        if (!await boardRepository.ExistsAsync(boardId, cancellationToken))
        {
            return null;
        }

        return await boardRepository.RunBoardOrderingAsync<WorkflowColumnResponseDto?>(
            boardId,
            async token =>
            {
                var columns = await boardRepository.GetBoardColumnsForUpdateAsync(boardId, token);

                var column = new WorkflowColumn
                {
                    BoardId = boardId,

                    Name = request.Name.Trim(),

                    SystemKind = null
                };

                boardRepository.AddColumn(column);

                columns.Add(column);
                ApplyCanonicalOrder(columns);

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

        var tasks = await taskRepository.GetTasksWithDetailsAsync([columnId], cancellationToken);

        return (RenameColumnOutcome.Renamed, column.ToResponseDto(tasks));
    }

    public async Task<DeleteColumnOutcome> DeleteAsync(int columnId, CancellationToken cancellationToken)
    {
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
                var tracked = await boardRepository.GetColumnForUpdateAsync(columnId, token);

                if (tracked is null)
                {
                    return DeleteColumnOutcome.ColumnNotFound;
                }

                if (tracked.SystemKind is not null)
                {
                    return DeleteColumnOutcome.ColumnIsSystem;
                }

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

        return await boardRepository.RunBoardOrderingAsync(
            boardId,
            async token =>
            {
                var columns = await boardRepository.GetBoardColumnsForUpdateAsync(boardId, token);
                var requestedIds = request.OrderedColumnIds;

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

    private static WorkflowColumn RequireSystemColumn(List<WorkflowColumn> columns, SystemColumnKind kind)
        => columns.FirstOrDefault(column => column.SystemKind == kind)
           ?? throw new InvalidOperationException(
               $"Board is missing its {kind} column, which every board is required to have.");
}
