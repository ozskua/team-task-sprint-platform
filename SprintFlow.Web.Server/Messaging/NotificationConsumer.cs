using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SprintFlow.Web.Server.Services;

namespace SprintFlow.Web.Server.Messaging;

/// <summary>
/// Reads notification events off the queue and turns each one into notification rows.
/// </summary>
/// <remarks>
/// <para>
/// It writes nothing itself. Persisting a notification — and telling the recipients over SignalR —
/// stays in <see cref="NotificationService"/>, which is the same method a direct call used before
/// the broker existed. That is what makes the queue a delivery mechanism rather than a second
/// implementation of the feature.
/// </para>
/// <para>
/// A hosted service is a singleton while <see cref="NotificationService"/> and the repositories
/// under it are scoped, so a scope is created per message rather than injecting them here — a
/// DbContext must not be shared across messages. The same arrangement
/// <c>AdminDashboardRefreshService</c> uses per turn.
/// </para>
/// <para>
/// Acknowledgement is manual. A message is removed from the queue only once its rows are committed,
/// so a crash between delivery and commit leaves it queued for the next start rather than losing
/// it.
/// </para>
/// </remarks>
public class NotificationConsumer : BackgroundService
{
    /// <summary>
    /// How long to wait before trying again when the broker cannot be reached at start-up. Long
    /// enough not to spin, short enough that a broker started after the application is picked up
    /// without a restart.
    /// </summary>
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(10);

    /// <summary>
    /// One message at a time. The work per message is a handful of inserts, and a prefetch of one
    /// means an unacknowledged message is never sitting in this process while another is worked on.
    /// </summary>
    private const ushort PrefetchCount = 1;

    private readonly RabbitMqConnection connection;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<NotificationConsumer> logger;

    public NotificationConsumer(
        RabbitMqConnection connection,
        IServiceScopeFactory scopeFactory,
        ILogger<NotificationConsumer> logger)
    {
        this.connection = connection;
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Shutting down, not a failure.
                return;
            }
            catch (Exception exception)
            {
                // Almost always the broker being unreachable. Caught so the application keeps
                // serving requests without it, and so the subscription is rebuilt when it returns.
                logger.LogWarning(
                    exception,
                    "The notification consumer could not run. Retrying in {Seconds} seconds.",
                    RetryDelay.TotalSeconds);
            }

            try
            {
                await Task.Delay(RetryDelay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    /// <summary>
    /// Subscribes and stays subscribed. Returns only when the channel closes or the application is
    /// stopping, which sends the loop above around again.
    /// </summary>
    private async Task ConsumeAsync(CancellationToken stoppingToken)
    {
        var open = await connection.GetAsync(stoppingToken);

        await using var channel = await open.CreateChannelAsync(cancellationToken: stoppingToken);

        // Declared here too, not only by the publisher: whichever side connects first creates the
        // topology, so neither has to be started before the other.
        await NotificationTopology.DeclareAsync(channel, stoppingToken);

        await channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: PrefetchCount,
            global: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += (_, delivery) => HandleAsync(channel, delivery, stoppingToken);

        await channel.BasicConsumeAsync(
            queue: NotificationTopology.QueueName,
            // Manual: the broker keeps the message until the rows are committed.
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        logger.LogInformation(
            "Listening for notification events on {Queue}.",
            NotificationTopology.QueueName);

        // The subscription lives on the channel, so this method has to stay here holding it open.
        while (!stoppingToken.IsCancellationRequested && channel.IsOpen)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }

    private async Task HandleAsync(
        IChannel channel,
        BasicDeliverEventArgs delivery,
        CancellationToken stoppingToken)
    {
        NotificationEvent? notificationEvent;

        try
        {
            notificationEvent = JsonSerializer.Deserialize<NotificationEvent>(delivery.Body.Span);
        }
        catch (JsonException exception)
        {
            // A message this application cannot read will never become readable, so requeueing it
            // would put it straight back at the head of the queue and block everything behind it.
            // Dropped and logged loudly instead. There is no dead-letter queue in this version.
            logger.LogError(
                exception,
                "A notification event could not be read and was discarded. Message {MessageId}.",
                delivery.BasicProperties.MessageId);

            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, stoppingToken);

            return;
        }

        if (notificationEvent is null || notificationEvent.RecipientUserIds.Count == 0)
        {
            // Nothing to write. Acknowledged rather than requeued, for the same reason as above:
            // redelivering it would produce the same nothing.
            logger.LogWarning(
                "A notification event carried no recipients and was skipped. Message {MessageId}.",
                delivery.BasicProperties.MessageId);

            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, stoppingToken);

            return;
        }

        try
        {
            // A scope per message: NotificationService and its repository are scoped, and the
            // DbContext beneath them is not safe to reuse across messages.
            await using var scope = scopeFactory.CreateAsyncScope();

            var notifications = scope.ServiceProvider.GetRequiredService<NotificationService>();

            await notifications.CreateManyAsync(
                notificationEvent.RecipientUserIds,
                notificationEvent.TaskId,
                notificationEvent.Type,
                notificationEvent.ActorUserId,
                stoppingToken);

            // Only now is the message gone from the queue.
            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, stoppingToken);

            logger.LogInformation(
                "Wrote {Count} notification(s) for task {TaskId}. Event {EventId}.",
                notificationEvent.RecipientUserIds.Count,
                notificationEvent.TaskId,
                notificationEvent.EventId);
        }
        catch (Exception exception)
        {
            // Typically SQL Server being unavailable, which passes. Requeued so it is tried again
            // rather than lost — the cost of which is that a message can be delivered more than
            // once, and this version has no duplicate check. See the roadmap's limitations.
            logger.LogError(
                exception,
                "A notification event could not be saved and was requeued. Event {EventId}.",
                notificationEvent.EventId);

            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: true, stoppingToken);
        }
    }
}
