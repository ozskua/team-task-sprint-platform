using RabbitMQ.Client;

namespace SprintFlow.Web.Notification.Messaging;

public static class RealtimeTopology
{
    public const string ExchangeName = "sprintflow.realtime";

    public const string QueueName = "sprintflow.realtime";

    public const string NotificationDeliveredKey = "notification.delivered";

    public const string BoardChangedKey = "board.changed";

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

        foreach (var routingKey in new[] { NotificationDeliveredKey, BoardChangedKey })
        {
            await channel.QueueBindAsync(
                queue: QueueName,
                exchange: ExchangeName,
                routingKey: routingKey,
                arguments: null,
                cancellationToken: cancellationToken);
        }
    }
}
