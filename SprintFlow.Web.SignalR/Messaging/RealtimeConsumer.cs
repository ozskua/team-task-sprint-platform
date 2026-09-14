using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SprintFlow.Web.SignalR.Hubs;

namespace SprintFlow.Web.SignalR.Messaging;

/// <summary>
/// Reads realtime events off the queue and tells the browsers connected to this service.
/// </summary>
/// <remarks>
/// <para>
/// The whole of this service's business logic, and there is deliberately almost none: the message
/// already says who should be told, and this turns that into a hub call. It makes no domain
/// decision, reads no database and never asks another service anything. If the payload did not name
/// its audience, this class would have to — and that is exactly the responsibility it must not
/// have.
/// </para>
/// <para>
/// <b>Delivery is best effort, and that is not a failure mode.</b> SignalR drops a message for a
/// user who is not connected, silently and correctly: the notification is already in the database
/// and the client will read it over REST. So a message is acknowledged once delivery has been
/// attempted, never requeued. Requeueing would not make an absent browser present; it would only
/// block the queue behind it.
/// </para>
/// <para>
/// No scope is created per message, unlike the notification service's consumer: an
/// <c>IHubContext</c> is a singleton and nothing here is scoped, because nothing here touches a
/// DbContext.
/// </para>
/// </remarks>
public class RealtimeConsumer : BackgroundService
{
    /// <summary>
    /// How long to wait before trying again when the broker cannot be reached. Long enough not to
    /// spin, short enough that a broker started after this service is picked up without a restart.
    /// </summary>
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Delivery is cheap — a hub call over open sockets — so a handful in flight is fine. Higher
    /// than the notification service's one, which is bounded by database writes instead.
    /// </summary>
    private const ushort PrefetchCount = 16;

    private readonly RabbitMqConnection connection;
    private readonly IHubContext<NotificationHub> notificationHub;
    private readonly IHubContext<BoardHub> boardHub;
    private readonly ILogger<RealtimeConsumer> logger;

    public RealtimeConsumer(
        RabbitMqConnection connection,
        IHubContext<NotificationHub> notificationHub,
        IHubContext<BoardHub> boardHub,
        ILogger<RealtimeConsumer> logger)
    {
        this.connection = connection;
        this.notificationHub = notificationHub;
        this.boardHub = boardHub;
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
                // Almost always the broker being unreachable. Caught so the service keeps serving
                // its WebSocket connections without it, and so the subscription is rebuilt when it
                // returns.
                logger.LogWarning(
                    exception,
                    "The realtime consumer could not run. Retrying in {Seconds} seconds.",
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
        await RealtimeTopology.DeclareAsync(channel, stoppingToken);

        await channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: PrefetchCount,
            global: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += (_, delivery) => HandleAsync(channel, delivery, stoppingToken);

        await channel.BasicConsumeAsync(
            queue: RealtimeTopology.QueueName,
            // Manual, so a message is not lost if this service stops between delivery and the send.
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        logger.LogInformation(
            "Listening for realtime events on {Queue}.",
            RealtimeTopology.QueueName);

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
        // The routing key is what says which kind of message this is, so nothing has to be
        // deserialised twice or guessed at from its shape.
        var routingKey = delivery.RoutingKey;

        try
        {
            switch (routingKey)
            {
                case RealtimeTopology.NotificationDeliveredKey:
                    await DeliverNotificationAsync(delivery, stoppingToken);
                    break;

                case RealtimeTopology.BoardChangedKey:
                    await DeliverBoardChangeAsync(delivery, stoppingToken);
                    break;

                default:
                    // Bound to this queue but not understood here. Dropped rather than requeued:
                    // redelivering it would produce the same nothing, forever.
                    logger.LogWarning(
                        "A realtime event arrived with an unknown routing key {RoutingKey} and was discarded.",
                        routingKey);

                    await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, stoppingToken);
                    return;
            }
        }
        catch (JsonException exception)
        {
            // A message this service cannot read will never become readable, so requeueing it would
            // put it straight back at the head of the queue and block everything behind it. Dropped
            // and logged loudly instead. There is no dead-letter queue in this version.
            logger.LogError(
                exception,
                "A realtime event on {RoutingKey} could not be read and was discarded. Message {MessageId}.",
                routingKey,
                delivery.BasicProperties.MessageId);

            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, stoppingToken);
            return;
        }
        catch (Exception exception)
        {
            // The send itself failed. Still acknowledged below: the notification is already durable
            // in the database and the client reads it over REST, so retrying delivery would gain
            // nothing and risk blocking the queue.
            logger.LogWarning(
                exception,
                "A realtime event on {RoutingKey} could not be delivered. The data behind it is unaffected.",
                routingKey);
        }

        await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, stoppingToken);
    }

    /// <summary>
    /// Tells each recipient that something new is waiting. The event carries no payload, for the
    /// same reason it never did: the client re-reads over REST, which stays the source of truth.
    /// </summary>
    private async Task DeliverNotificationAsync(
        BasicDeliverEventArgs delivery,
        CancellationToken stoppingToken)
    {
        var message = JsonSerializer.Deserialize<NotificationDeliveredEvent>(delivery.Body.Span)
            ?? throw new JsonException("The notification delivery event deserialised to null.");

        if (message.RecipientUserIds.Count == 0)
        {
            logger.LogWarning(
                "A notification delivery event named no recipients. Event {EventId}.",
                message.EventId);

            return;
        }

        await notificationHub.Clients
            .Users(message.RecipientUserIds.Select(id => id.ToString()).ToList())
            .SendAsync("notificationReceived", stoppingToken);

        logger.LogInformation(
            "Delivered a notification to {Count} recipient(s). Event {EventId}.",
            message.RecipientUserIds.Count,
            message.EventId);
    }

    /// <summary>
    /// Tells everyone watching one board to re-read it. The group name and the event name are the
    /// ones the browser already listens for; only where the instruction comes from has changed.
    /// </summary>
    private async Task DeliverBoardChangeAsync(
        BasicDeliverEventArgs delivery,
        CancellationToken stoppingToken)
    {
        var message = JsonSerializer.Deserialize<BoardChangedEvent>(delivery.Body.Span)
            ?? throw new JsonException("The board changed event deserialised to null.");

        await boardHub.Clients
            .Group(BoardHub.GroupFor(message.BoardId))
            .SendAsync("boardChanged", new { boardId = message.BoardId, reason = message.Reason }, stoppingToken);

        logger.LogInformation(
            "Delivered {Reason} for board {BoardId}. Event {EventId}.",
            message.Reason,
            message.BoardId,
            message.EventId);
    }
}
