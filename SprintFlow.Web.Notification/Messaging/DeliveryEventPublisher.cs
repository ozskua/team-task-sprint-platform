using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace SprintFlow.Web.Notification.Messaging;

public class DeliveryEventPublisher
{
    private readonly RabbitMqConnection connection;
    private readonly ILogger<DeliveryEventPublisher> logger;

    private readonly SemaphoreSlim gate = new(1, 1);

    private IChannel? channel;

    public DeliveryEventPublisher(RabbitMqConnection connection, ILogger<DeliveryEventPublisher> logger)
    {
        this.connection = connection;
        this.logger = logger;
    }

    public async Task AnnounceAsync(
        Guid eventId,
        IReadOnlyList<int> recipientUserIds,
        CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);

        try
        {
            var publishChannel = await EnsureChannelAsync(cancellationToken);

            var message = new NotificationDeliveredEvent(eventId, recipientUserIds);

            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                MessageId = eventId.ToString()
            };

            await publishChannel.BasicPublishAsync(
                exchange: RealtimeTopology.ExchangeName,
                routingKey: RealtimeTopology.NotificationDeliveredKey,
                mandatory: false,
                basicProperties: properties,
                body: JsonSerializer.SerializeToUtf8Bytes(message),
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "Announced delivery to {Count} recipient(s). Event {EventId}.",
                recipientUserIds.Count,
                eventId);
        }
        catch (Exception exception)
        {
            await DiscardChannelAsync();

            logger.LogWarning(
                exception,
                "The realtime announcement for event {EventId} failed. The notifications are saved and readable over REST.",
                eventId);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<IChannel> EnsureChannelAsync(CancellationToken cancellationToken)
    {
        if (channel is { IsOpen: true })
        {
            return channel;
        }

        if (channel is not null)
        {
            await channel.DisposeAsync();
            channel = null;
        }

        var open = await connection.GetAsync(cancellationToken);

        channel = await open.CreateChannelAsync(cancellationToken: cancellationToken);

        await RealtimeTopology.DeclareAsync(channel, cancellationToken);

        return channel;
    }

    private async Task DiscardChannelAsync()
    {
        if (channel is null)
        {
            return;
        }

        try
        {
            await channel.DisposeAsync();
        }
        catch (Exception exception) when (exception is BrokerUnreachableException
            or AlreadyClosedException
            or OperationInterruptedException
            or IOException)
        {
        }

        channel = null;
    }
}
