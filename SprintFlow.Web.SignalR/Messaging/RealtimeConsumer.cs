using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SprintFlow.Web.SignalR.Hubs;

namespace SprintFlow.Web.SignalR.Messaging;

public class RealtimeConsumer : BackgroundService
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(10);

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
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        logger.LogInformation(
            "Listening for realtime events on {Queue}.",
            RealtimeTopology.QueueName);

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
                    logger.LogWarning(
                        "A realtime event arrived with an unknown routing key {RoutingKey} and was discarded.",
                        routingKey);

                    await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, stoppingToken);
                    return;
            }
        }
        catch (JsonException exception)
        {
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
            logger.LogWarning(
                exception,
                "A realtime event on {RoutingKey} could not be delivered. The data behind it is unaffected.",
                routingKey);
        }

        await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, stoppingToken);
    }

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
