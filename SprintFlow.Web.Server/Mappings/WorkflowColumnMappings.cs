using SprintFlow.Web.Server.DTOs.Columns;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Mappings;

public static class WorkflowColumnMappings
{
    public static WorkflowColumnResponseDto ToResponseDto(
        this WorkflowColumn column,
        IEnumerable<TaskItem> tasks)
        => new(
            column.WorkflowColumnId,
            column.BoardId,
            column.Name,
            column.Position,
            column.SystemKind?.ToString(),
            tasks.ToOrderedResponseDtos());

    public static WorkflowColumnResponseDto ToResponseDto(this WorkflowColumn column)
        => column.ToResponseDto([]);

    public static IReadOnlyList<WorkflowColumnResponseDto> ToOrderedResponseDtos(
        this IEnumerable<WorkflowColumn> columns,
        ILookup<int, TaskItem> tasksByColumn)
        => columns
            .OrderBy(column => column.Position)
            .ThenBy(column => column.WorkflowColumnId)
            .Select(column => column.ToResponseDto(tasksByColumn[column.WorkflowColumnId]))
            .ToList();
}
