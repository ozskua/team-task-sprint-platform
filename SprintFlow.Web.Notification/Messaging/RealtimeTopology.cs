using RabbitMQ.Client;

namespace SprintFlow.Web.Notification.Messaging;

/// <summary>
/// The exchange this service publishes board changes to, and the code that creates it.
/// </summary>
/// <remarks>
/// <para>
/// This application's own copy of the topology. The realtime service declares the same names for
/// itself; they are a contract between processes rather than shared code, and
/// <c>docs/EVENT_CONTRACTS.md</c> is the authority when the two are compared.
/// </para>
/// <para>
/// The queue and both bindings are declared here as well, even though nothing in this process
/// reads them. A direct exchange discards a message that no queue is bound to, so if the realtime
/// service had never started, every board change published before it did would be silently thrown
/// away. Declaring the queue from the publishing side means the messages wait instead.
/// </para>
/// </remarks>
public static class RealtimeTopology
{
    /// <summary>Everything the browser has to be told about, whoever noticed it.</summary>
    public const string ExchangeName = "sprintflow.realtime";

    public const string QueueName = "sprintflow.realtime";

    /// <summary>Published by the notification service once a notification is durable.</summary>
    public const string NotificationDeliveredKey = "notification.delivered";

    /// <summary>Published here, when a board changed and its watchers should re-read it.</summary>
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
