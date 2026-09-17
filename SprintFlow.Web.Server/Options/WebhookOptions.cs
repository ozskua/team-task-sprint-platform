namespace SprintFlow.Web.Server.Options;

public class WebhookOptions
{
    public const string SectionName = "Webhooks";

    public string Secret { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 10;

    public bool AllowHttpLocalhost { get; set; }
}
