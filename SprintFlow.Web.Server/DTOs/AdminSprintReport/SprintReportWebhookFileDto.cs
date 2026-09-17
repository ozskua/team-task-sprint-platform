namespace SprintFlow.Web.Server.DTOs.AdminSprintReport;

public record SprintReportWebhookFileDto(
    string Format,
    string FileName,
    long SizeBytes,
    string Sha256,
    string ContentType);
