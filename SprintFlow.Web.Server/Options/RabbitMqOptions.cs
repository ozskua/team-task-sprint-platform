namespace SprintFlow.Web.Server.Options;

// Where the broker is and who to connect as. The values live in appsettings.Development.json, which
// is git-ignored, and every one of them can be overridden by an environment variable
// (RabbitMQ__Host, RabbitMQ__Password, and so on) the way .NET configuration always allows — so a
// different machine needs no file edit and no credential is ever committed.
public class RabbitMqOptions
{
    public const string SectionName = "RabbitMQ";

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 5672;

    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    /// <summary>The default vhost. Named here rather than assumed, because "/" is easy to mistype.</summary>
    public string VirtualHost { get; set; } = "/";
}
