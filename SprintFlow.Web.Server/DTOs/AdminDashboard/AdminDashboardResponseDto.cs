namespace SprintFlow.Web.Server.DTOs.AdminDashboard;

public record AdminDashboardResponseDto(
    AdminDashboardCountsDto Counts,
    IReadOnlyList<AdminDashboardTaskDto> Tasks);
