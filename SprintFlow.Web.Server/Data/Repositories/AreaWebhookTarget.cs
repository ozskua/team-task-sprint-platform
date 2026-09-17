namespace SprintFlow.Web.Server.Data.Repositories;

public sealed record AreaWebhookTarget(int AreaId, string Name, string? WebhookUrl);
