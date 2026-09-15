using RabbitMQ.Client;

namespace SprintFlow.Web.Server.Messaging;

public static class NotificationTopology
{
    public const string ExchangeName = "sprintflow.notifications";

    public const string QueueName = "sprintflow.notifications";

    public const string RoutingKey = "notification.created";

    public static async Task DeclareAsync(IChannel channel, CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: QueueName,
            exchange: ExchangeName,
            routingKey: RoutingKey,
            arguments: null,
            cancellationToken: cancellationToken);
    }
}
