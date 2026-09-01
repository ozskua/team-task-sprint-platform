using SprintFlow.Web.Server.DTOs.Tasks;

namespace SprintFlow.Web.Server.DTOs.Columns;

public record WorkflowColumnResponseDto(
    int WorkflowColumnId,
    int BoardId,
    string Name,
    int Position,
    string? SystemKind,
    IReadOnlyList<TaskResponseDto> Tasks);
