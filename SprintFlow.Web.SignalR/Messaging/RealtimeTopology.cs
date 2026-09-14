using RabbitMQ.Client;

namespace SprintFlow.Web.SignalR.Messaging;

/// <summary>
/// The exchange, queue and routing keys this service listens on, and the code that creates them.
/// </summary>
/// <remarks>
/// <para>
/// Declared on every start, and also by whoever publishes. Declaring is idempotent in RabbitMQ, so
/// no service has to be started before another and a broker restarted with an empty data volume
/// gets its topology back on the next connection.
/// </para>
/// <para>
/// <b>A queue of its own, not a second consumer on the notification queue.</b> Two consumers on one
/// queue split the messages between them, which would mean roughly half the notifications were
/// persisted and the other half delivered. Realtime delivery is a different job from persistence,
/// so it gets a different queue — and every message reaches both.
/// </para>
/// <para>
/// These names are declared here and, independently, in the services that publish. They are a
/// contract between processes rather than shared code; <c>docs/EVENT_CONTRACTS.md</c> is the
/// authority when the two are compared.
/// </para>
/// </remarks>
public static class RealtimeTopology
{
    /// <summary>Everything the browser has to be told about, whoever noticed it.</summary>
    public const string ExchangeName = "sprintflow.realtime";

    public const string QueueName = "sprintflow.realtime";

    /// <summary>A notification was persisted and its recipients should be told. From the notification service.</summary>
    public const string NotificationDeliveredKey = "notification.delivered";

    /// <summary>A board changed and whoever is watching it should re-read. From the API service.</summary>
    public const string BoardChangedKey = "board.changed";

    /// <summary>
    /// Creates the exchange, the queue and both bindings, if they are not already there.
    /// </summary>
    /// <remarks>
    /// Durable and not auto-delete, with persistent messages, so a notification is not lost because
    /// this service happened to be restarting. One queue carries both routing keys because one
    /// consumer wants both.
    /// </remarks>
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
            // Not exclusive: the queue belongs to this service, not to whichever connection
            // happened to declare it, so a restart finds the messages it left behind.
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
