namespace SprintFlow.Web.Server.DTOs.AdminSprintReport;

public record SprintReportWebhookSummaryDto(
    int Sprints,
    int Tasks,
    int Completed,
    int Cancelled,
    int Ongoing);
