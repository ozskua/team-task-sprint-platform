namespace SprintFlow.Web.Notification.Options;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMQ";

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 5672;

    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string VirtualHost { get; set; } = "/";
}
