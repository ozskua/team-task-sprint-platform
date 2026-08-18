using SprintFlow.Web.Server.DTOs.Columns;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Mappings;

public static class WorkflowColumnMappings
{
    /// <summary>
    /// Maps a column and whatever tasks it currently holds. The kind travels as its name rather
    /// than its number, so a client reads <c>"ToDo"</c> instead of <c>1</c> and never has to keep a
    /// copy of the enum in step with the server.
    /// </summary>
    /// <remarks>
    /// Requires each task's creator and participants to be loaded — see <see cref="TaskMappings"/>.
    /// </remarks>
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

    /// <summary>A column with no tasks attached — the shape returned right after one is created.</summary>
    public static WorkflowColumnResponseDto ToResponseDto(this WorkflowColumn column)
        => column.ToResponseDto([]);

    /// <summary>
    /// Columns in board order: To Do, then the custom columns, then Completed and Cancelled. Ties
    /// are broken by id for the same reason tasks' are.
    /// </summary>
    public static IReadOnlyList<WorkflowColumnResponseDto> ToOrderedResponseDtos(
        this IEnumerable<WorkflowColumn> columns,
        ILookup<int, TaskItem> tasksByColumn)
        => columns
            .OrderBy(column => column.Position)
            .ThenBy(column => column.WorkflowColumnId)
            .Select(column => column.ToResponseDto(tasksByColumn[column.WorkflowColumnId]))
            .ToList();
}
