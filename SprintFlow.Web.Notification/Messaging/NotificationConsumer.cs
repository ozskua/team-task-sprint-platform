using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SprintFlow.Web.Notification.Services;

namespace SprintFlow.Web.Notification.Messaging;

/// <summary>
/// Reads notification events off the queue, writes the rows, and announces them.
/// </summary>
/// <remarks>
/// <para>
/// A hosted service is a singleton while <see cref="NotificationWriter"/> and the DbContext under
/// it are scoped, so a scope is created per message — a DbContext must not be shared across
/// messages.
/// </para>
/// <para>
/// <b>Two failures, two different answers.</b> A database failure means the work has not been done
/// and is worth retrying, so the message is requeued. A failed announcement means the work
/// <em>has</em> been done and only the live update was lost, so the message is acknowledged
/// anyway. Requeueing on the second would replay the insert and duplicate the notifications — the
/// acknowledgement therefore happens the moment the rows are durable, before anything else is
/// attempted.
/// </para>
/// </remarks>
public class NotificationConsumer : BackgroundService
{
    /// <summary>
    /// How long to wait before trying again when the broker cannot be reached. Long enough not to
    /// spin, short enough that a broker started after this service is picked up without a restart.
    /// </summary>
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(10);

    /// <summary>
    /// One message at a time. The work per message is a handful of inserts, and a prefetch of one
    /// means an unacknowledged message is never sitting here while another is worked on.
    /// </summary>
    private const ushort PrefetchCount = 1;

    private readonly RabbitMqConnection connection;
    private readonly DeliveryEventPublisher deliveryPublisher;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<NotificationConsumer> logger;

    public NotificationConsumer(
        RabbitMqConnection connection,
        DeliveryEventPublisher deliveryPublisher,
        IServiceScopeFactory scopeFactory,
        ILogger<NotificationConsumer> logger)
    {
        this.connection = connection;
        this.deliveryPublisher = deliveryPublisher;
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
                return;
            }
            catch (Exception exception)
            {
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

    private async Task ConsumeAsync(CancellationToken stoppingToken)
    {
        var open = await connection.GetAsync(stoppingToken);

        await using var channel = await open.CreateChannelAsync(cancellationToken: stoppingToken);

        // Declared here too, so whichever service connects first creates the topology.
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
            // A message this service cannot read will never become readable, so requeueing it would
            // put it straight back at the head of the queue and block everything behind it. Dropped
            // and logged loudly instead. There is no dead-letter queue in this version.
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

        // --- Step one: persist. A failure here is worth retrying. ---------------------------
        try
        {
            // A scope per message: the writer and its DbContext are scoped, and a DbContext is not
            // safe to reuse across messages.
            await using var scope = scopeFactory.CreateAsyncScope();

            var writer = scope.ServiceProvider.GetRequiredService<NotificationWriter>();

            await writer.WriteAsync(
                notificationEvent.RecipientUserIds,
                notificationEvent.TaskId,
                notificationEvent.Type,
                notificationEvent.ActorUserId,
                stoppingToken);
        }
        catch (Exception exception)
        {
            // Typically SQL Server being unavailable. Requeued so it is tried again rather than
            // lost — the cost of which is that a message can be delivered more than once, and this
            // version has no duplicate check. See the roadmap's limitations.
            logger.LogError(
                exception,
                "A notification event could not be saved and was requeued. Event {EventId}.",
                notificationEvent.EventId);

            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: true, stoppingToken);

            return;
        }

        // --- The rows are durable. The message's job is done, so acknowledge before anything
        //     else is attempted: from here on, no failure may cause a redelivery. ---------------
        await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, stoppingToken);

        logger.LogInformation(
            "Wrote {Count} notification(s) for task {TaskId}. Event {EventId}.",
            notificationEvent.RecipientUserIds.Count,
            notificationEvent.TaskId,
            notificationEvent.EventId);

        // --- Step two: announce. Best effort, and it swallows its own failures. ---------------
        await deliveryPublisher.AnnounceAsync(
            notificationEvent.EventId,
            notificationEvent.RecipientUserIds,
            stoppingToken);
    }
}
