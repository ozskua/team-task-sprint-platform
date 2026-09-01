namespace SprintFlow.Web.Server.DTOs.Dashboard;

public record DashboardTaskCountsDto(
    int ToDo,
    int Open,
    int Completed,
    int Cancelled);
