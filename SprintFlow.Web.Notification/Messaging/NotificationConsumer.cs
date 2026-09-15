using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SprintFlow.Web.Notification.Services;

namespace SprintFlow.Web.Notification.Messaging;

public class NotificationConsumer : BackgroundService
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(10);

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
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        logger.LogInformation(
            "Listening for notification events on {Queue}.",
            NotificationTopology.QueueName);

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
            logger.LogError(
                exception,
                "A notification event could not be read and was discarded. Message {MessageId}.",
                delivery.BasicProperties.MessageId);

            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, stoppingToken);

            return;
        }

        if (notificationEvent is null || notificationEvent.RecipientUserIds.Count == 0)
        {
            logger.LogWarning(
                "A notification event carried no recipients and was skipped. Message {MessageId}.",
                delivery.BasicProperties.MessageId);

            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, stoppingToken);

            return;
        }

        try
        {
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
            logger.LogError(
                exception,
                "A notification event could not be saved and was requeued. Event {EventId}.",
                notificationEvent.EventId);

            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: true, stoppingToken);

            return;
        }

        await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, stoppingToken);

        logger.LogInformation(
            "Wrote {Count} notification(s) for task {TaskId}. Event {EventId}.",
            notificationEvent.RecipientUserIds.Count,
            notificationEvent.TaskId,
            notificationEvent.EventId);

        await deliveryPublisher.AnnounceAsync(
            notificationEvent.EventId,
            notificationEvent.RecipientUserIds,
            stoppingToken);
    }
}
