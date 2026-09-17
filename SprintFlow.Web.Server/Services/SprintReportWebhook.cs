using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SprintFlow.Web.Server.Common.Enums;
using SprintFlow.Web.Server.Data.Repositories;
using SprintFlow.Web.Server.DTOs.AdminSprintReport;
using SprintFlow.Web.Server.Options;

namespace SprintFlow.Web.Server.Services;

public class SprintReportWebhook
{
    public const string HttpClientName = "SprintReportWebhook";

    public const string EventName = "sprint.report.created";

    public const string ResultHeader = "X-SprintFlow-Webhook";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AreaWebhookService areaWebhooks;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly IApplicationClock clock;
    private readonly WebhookOptions options;
    private readonly ILogger<SprintReportWebhook> logger;

    public SprintReportWebhook(
        AreaWebhookService areaWebhooks,
        IHttpClientFactory httpClientFactory,
        IApplicationClock clock,
        IOptions<WebhookOptions> options,
        ILogger<SprintReportWebhook> logger)
    {
        this.areaWebhooks = areaWebhooks;
        this.httpClientFactory = httpClientFactory;
        this.clock = clock;
        this.options = options.Value;
        this.logger = logger;
    }

    public async Task<SprintReportWebhookResult> DeliverAsync(
        SprintReportExport export,
        SprintReportFormat format,
        int producedByUserId,
        CancellationToken cancellationToken)
    {
        var targets = await areaWebhooks.GetTargetsAsync(cancellationToken);

        var delivered = 0;
        var failed = 0;
        var skipped = 0;

        foreach (var target in targets)
        {
            if (string.IsNullOrEmpty(target.WebhookUrl))
            {
                skipped++;

                continue;
            }

            if (await SendAsync(target, target.WebhookUrl, export.ForArea(target.AreaId), format, producedByUserId, cancellationToken))
            {
                delivered++;
            }
            else
            {
                failed++;
            }
        }

        return new SprintReportWebhookResult(delivered, failed, skipped);
    }

    private async Task<bool> SendAsync(
        AreaWebhookTarget target,
        string url,
        SprintReportExport areaExport,
        SprintReportFormat format,
        int producedByUserId,
        CancellationToken cancellationToken)
    {
        var eventId = Guid.NewGuid();
        var occurredAt = DateTime.UtcNow;
        var file = SprintReportFile.Render(areaExport, format, clock.Today(), target.AreaId);
        var fileHash = Convert.ToHexStringLower(SHA256.HashData(file.Bytes));

        var metadata = new SprintReportWebhookMetadataDto(
            eventId,
            EventName,
            occurredAt,
            target.AreaId,
            target.Name,
            new SprintReportWebhookFileDto(
                format.ToString().ToLowerInvariant(),
                file.FileName,
                file.Bytes.Length,
                fileHash,
                file.ContentType),
            new SprintReportWebhookSummaryDto(
                areaExport.Rows.Count,
                areaExport.TotalTasks,
                areaExport.CompletedTasks,
                areaExport.CancelledTasks,
                areaExport.OngoingTasks),
            producedByUserId);

        var metadataJson = JsonSerializer.Serialize(metadata, JsonOptions);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var signature = Sign($"{timestamp}.{eventId}.{fileHash}.{metadataJson}");

        using var content = new MultipartFormDataContent();

        var metadataPart = new StringContent(metadataJson, Encoding.UTF8, "application/json");
        content.Add(metadataPart, "metadata");

        var filePart = new ByteArrayContent(file.Bytes);
        filePart.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        content.Add(filePart, "file", file.FileName);

        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        request.Headers.Add("X-SprintFlow-Event", EventName);
        request.Headers.Add("X-SprintFlow-Event-Id", eventId.ToString());
        request.Headers.Add("X-SprintFlow-Timestamp", timestamp);
        request.Headers.Add("X-SprintFlow-Signature", $"sha256={signature}");

        var host = new Uri(url).Host;

        try
        {
            using var client = httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation(
                    "Delivered sprint report ({Format}, {Size} bytes) for area {AreaId} to {Host} with status {Status}. Event {EventId}.",
                    file.ContentType,
                    file.Bytes.Length,
                    target.AreaId,
                    host,
                    (int)response.StatusCode,
                    eventId);

                return true;
            }

            logger.LogWarning(
                "Sprint report webhook for area {AreaId} to {Host} was rejected with status {Status}. Event {EventId}.",
                target.AreaId,
                host,
                (int)response.StatusCode,
                eventId);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                "Sprint report webhook for area {AreaId} to {Host} timed out after {Seconds} seconds. Event {EventId}.",
                target.AreaId,
                host,
                options.TimeoutSeconds,
                eventId);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(
                exception,
                "Sprint report webhook for area {AreaId} to {Host} could not be sent. Event {EventId}.",
                target.AreaId,
                host,
                eventId);
        }

        return false;
    }

    private string Sign(string message)
        => Convert.ToHexStringLower(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(options.Secret), Encoding.UTF8.GetBytes(message)));
}
