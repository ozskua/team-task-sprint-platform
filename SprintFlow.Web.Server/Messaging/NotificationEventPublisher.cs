using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace SprintFlow.Web.Server.Messaging;

/// <summary>
/// Puts a notification event on the queue. The only place in the application that publishes.
/// </summary>
/// <remarks>
/// <para>
/// This exists so <c>TaskService</c> can say "this happened, tell these people" without holding a
/// channel, naming an exchange or knowing that RabbitMQ is involved at all. It is deliberately not
/// a generic bus: it publishes one kind of message, and a second kind would get its own small
/// publisher rather than a type parameter here.
/// </para>
/// <para>
/// <b>Failure is survivable and is swallowed.</b> The domain change has already been committed by
/// the time this runs, so throwing would report a failure for an operation that actually succeeded.
/// A broker that cannot be reached costs the notification and nothing else — the task, the comment
/// or the move stands, and the board's own realtime update still goes out. This is the same posture
/// the project already takes when Redis is unreachable, and the cost of it is recorded as the
/// dual-write limitation in the roadmap.
/// </para>
/// </remarks>
public class NotificationEventPublisher
{
    private readonly RabbitMqConnection connection;
    private readonly ILogger<NotificationEventPublisher> logger;

    /// <summary>
    /// Serialises publishing. Channels are not thread-safe, and two requests finishing at the same
    /// moment would otherwise use this one concurrently.
    /// </summary>
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
        // Nobody to tell. Publishing an event with no recipients would give the consumer nothing to
        // do, so it is not sent at all.
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
                // Written to disk by the broker, so a restart does not lose what is still queued.
                // Durable queue plus persistent message is what makes that promise hold; either
                // one alone does not.
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
            // The channel is likely unusable now; the next publish opens a fresh one.
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

    /// <summary>
    /// The publishing channel, opened once and reused. Declares the topology on the way, so a
    /// publish never targets an exchange that is not there.
    /// </summary>
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
            // Already broken, which is why it is being thrown away.
        }

        channel = null;
    }

    /// <summary>
    /// Narrow on purpose: a broker that is down is expected and survivable, but a serialisation
    /// defect or a bug here must keep travelling to the exception middleware rather than being
    /// logged as "RabbitMQ was unreachable". Same rule as the Redis cache's own filter.
    /// </summary>
    private static bool IsBrokerUnavailable(Exception exception)
        => exception is BrokerUnreachableException
            or AlreadyClosedException
            or OperationInterruptedException
            or ChannelAllocationException
            or TimeoutException
            or IOException;
}
