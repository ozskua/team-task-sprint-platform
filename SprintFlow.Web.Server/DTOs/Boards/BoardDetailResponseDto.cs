using SprintFlow.Web.Server.DTOs.Columns;
using SprintFlow.Web.Server.DTOs.Sprints;

namespace SprintFlow.Web.Server.DTOs.Boards;

public record BoardDetailResponseDto(
    int BoardId,
    string Name,
    int AreaId,
    int CreatedByUserId,
    string CreatedByFullName,
    DateTime CreatedDate,
    IReadOnlyList<TeamMemberResponseDto> TeamMembers,
    SprintResponseDto? SelectedSprint,
    IReadOnlyList<WorkflowColumnResponseDto> Columns);
