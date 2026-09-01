namespace SprintFlow.Web.Server.DTOs.Dashboard;

public record DashboardResponseDto(
    DashboardTaskCountsDto Counts,
    IReadOnlyList<DashboardTaskDto> MyTasks,
    IReadOnlyList<DashboardTeamDto> MyTeams);
