using SprintFlow.Web.Server.DTOs.Teams;

namespace SprintFlow.Web.Server.DTOs.Dashboard;

public record DashboardTeamDto(
    int BoardId,
    string BoardName,
    int AreaId,
    string AreaName,
    int MyAssignedTaskCount,
    IReadOnlyList<TeamMemberResponseDto> Members);
