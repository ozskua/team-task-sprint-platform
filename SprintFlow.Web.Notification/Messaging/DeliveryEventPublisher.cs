using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace SprintFlow.Web.Notification.Messaging;

/// <summary>
/// Tells the realtime service that a notification is durable and its recipients should be told.
/// </summary>
/// <remarks>
/// <para>
/// The second hop of the pipeline, and the reason this service needs no SignalR at all: it does not
/// deliver anything to a browser, it says that something is ready to be delivered. Whoever owns the
/// browser connections picks that up.
/// </para>
/// <para>
/// <b>A failure here must never undo the rows that were just written.</b> It is called after the
/// insert has committed, and its failure is logged and swallowed so the consumer can acknowledge
/// the message anyway. Requeueing on a failed announcement would replay the insert and produce
/// duplicate notifications — the exact defect this split was designed to prevent.
/// </para>
/// </remarks>
public class DeliveryEventPublisher
{
    private readonly RabbitMqConnection connection;
    private readonly ILogger<DeliveryEventPublisher> logger;

    /// <summary>Channels are not thread-safe, and the consumer may finish two messages at once.</summary>
    private readonly SemaphoreSlim gate = new(1, 1);

    private IChannel? channel;

    public DeliveryEventPublisher(RabbitMqConnection connection, ILogger<DeliveryEventPublisher> logger)
    {
        this.connection = connection;
        this.logger = logger;
    }

    /// <summary>
    /// Announces the delivery. Never throws: the caller has already committed and must be free to
    /// acknowledge.
    /// </summary>
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
            // Every failure, not only an unreachable broker: the rows are already committed and
            // nothing this method can do should put them at risk of being written twice. The cost
            // of the swallow is a lost live update, which the client recovers by reading over REST.
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
            // Already broken, which is why it is being thrown away.
        }

        channel = null;
    }
}
