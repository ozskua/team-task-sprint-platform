namespace SprintFlow.Web.Server.DTOs.AdminSprintReport;

public record SprintReportWebhookMetadataDto(
    Guid EventId,
    string Event,
    DateTime OccurredAt,
    int AreaId,
    string AreaName,
    SprintReportWebhookFileDto Report,
    SprintReportWebhookSummaryDto Summary,
    int ProducedByUserId);
