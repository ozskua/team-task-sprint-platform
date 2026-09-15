using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace SprintFlow.Web.Server.Messaging;

public class NotificationEventPublisher
{
    private readonly RabbitMqConnection connection;
    private readonly ILogger<NotificationEventPublisher> logger;

    private readonly SemaphoreSlim gate = new(1, 1);

    private IChannel? channel;

    public NotificationEventPublisher(
        RabbitMqConnection connection,
        ILogger<NotificationEventPublisher> logger)
    {
        this.connection = connection;
        this.logger = logger;
    }

    public async Task PublishAsync(NotificationEvent notificationEvent, CancellationToken cancellationToken)
    {
        if (notificationEvent.RecipientUserIds.Count == 0)
        {
            return;
        }

        await gate.WaitAsync(cancellationToken);

        try
        {
            var publishChannel = await EnsureChannelAsync(cancellationToken);

            var body = JsonSerializer.SerializeToUtf8Bytes(notificationEvent);

            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                MessageId = notificationEvent.EventId.ToString()
            };

            await publishChannel.BasicPublishAsync(
                exchange: NotificationTopology.ExchangeName,
                routingKey: NotificationTopology.RoutingKey,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "Published {Type} for task {TaskId} to {Count} recipient(s). Event {EventId}.",
                notificationEvent.Type,
                notificationEvent.TaskId,
                notificationEvent.RecipientUserIds.Count,
                notificationEvent.EventId);
        }
        catch (Exception exception) when (IsBrokerUnavailable(exception))
        {
            await DiscardChannelAsync();

            logger.LogWarning(
                exception,
                "RabbitMQ was unreachable, so the {Type} notification for task {TaskId} was not published. The change itself was saved.",
                notificationEvent.Type,
                notificationEvent.TaskId);
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

        await NotificationTopology.DeclareAsync(channel, cancellationToken);

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
        catch
        {
        }

        channel = null;
    }

    private static bool IsBrokerUnavailable(Exception exception)
        => exception is BrokerUnreachableException
            or AlreadyClosedException
            or OperationInterruptedException
            or ChannelAllocationException
            or TimeoutException
            or IOException;
}
