namespace SprintFlow.Web.Server.DTOs.AdminDashboard;

public record AdminDashboardCountsDto(
    int ToDo,
    int Open,
    int Completed,
    int Cancelled,
    int TotalUsers,
    int TotalBoards);
