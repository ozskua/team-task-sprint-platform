using RabbitMQ.Client;

namespace SprintFlow.Web.Notification.Messaging;

/// <summary>
/// The one exchange, queue and routing key notifications travel over, and the code that creates
/// them.
/// </summary>
/// <remarks>
/// Declared by both the publisher and the consumer, on every start. Declaring is idempotent in
/// RabbitMQ — asking for something that already exists with the same settings simply succeeds — so
/// neither side has to be started first, and a broker that was restarted with an empty data volume
/// gets its topology back on the next connection.
/// <para>
/// One direct exchange with one queue is the whole design. A topic exchange would let a future
/// consumer subscribe to a subset of the routing keys, but nothing does, and the four notification
/// kinds are already distinguished by a field inside the message.
/// </para>
/// </remarks>
public static class NotificationTopology
{
    /// <summary>Named after the application and what travels over it, like the Redis key prefix.</summary>
    public const string ExchangeName = "sprintflow.notifications";

    public const string QueueName = "sprintflow.notifications";

    /// <summary>What happened, not who wants it — the publisher never names a consumer.</summary>
    public const string RoutingKey = "notification.created";

    /// <summary>
    /// Creates the exchange, the queue and the binding between them, if they are not already there.
    /// </summary>
    /// <remarks>
    /// Both are <c>durable</c> and neither is <c>autoDelete</c>, so they survive a broker restart
    /// and outlive the consumer that reads them — which, together with persistent messages, is what
    /// stops a notification disappearing because the backend happened to be down.
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
            // Not exclusive: the queue belongs to the application, not to whichever connection
            // happened to declare it, so a restarted backend finds the messages it left behind.
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
