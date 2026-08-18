using SprintFlow.Web.Server.DTOs.Tasks;

namespace SprintFlow.Web.Server.DTOs.Columns;

/// <summary>A workflow column as returned to a client, with the tasks it holds in order.</summary>
/// <param name="WorkflowColumnId">Identifies the column.</param>
/// <param name="BoardId">The board this column belongs to.</param>
/// <param name="Name">Display name.</param>
/// <param name="Position">Zero-based slot on the board, decided by the server.</param>
/// <param name="SystemKind">
/// <c>null</c> for a custom column. Non-null names one of the three required columns, which is how
/// the client knows to offer neither rename, delete nor drag on it.
/// </param>
/// <param name="Tasks">The column's tasks, already ordered.</param>
public record WorkflowColumnResponseDto(
    int WorkflowColumnId,
    int BoardId,
    string Name,
    int Position,
    string? SystemKind,
    IReadOnlyList<TaskResponseDto> Tasks);
