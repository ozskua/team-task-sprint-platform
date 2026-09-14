using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace SprintFlow.Web.Server.Messaging;

/// <summary>
/// Announces that a board changed, so the realtime service can tell whoever is watching it.
/// </summary>
/// <remarks>
/// <para>
/// This replaces the direct <c>IHubContext&lt;BoardHub&gt;</c> call this application used to make.
/// The hub now lives in another process, so the announcement travels over the broker instead of
/// through memory. <c>TaskService</c> still calls one method at the same five places and still
/// passes a board id and a reason; only what happens behind that method changed.
/// </para>
/// <para>
/// <b>Failure is survivable and is swallowed.</b> The domain change has already been committed by
/// the time this runs, so throwing would report a failure for an operation that actually succeeded.
/// A broker that cannot be reached costs the live board refresh and nothing else — the change
/// stands, and the next thing that reloads the board shows it. Same posture, and the same narrow
/// exception filter, as <see cref="NotificationEventPublisher"/>.
/// </para>
/// <para>
/// This is the one real cost of moving the hub out: board updates used to reach the browser even
/// with RabbitMQ down, and now they do not. Recorded in the roadmap rather than hidden here.
/// </para>
/// </remarks>
public class BoardEventPublisher
{
    private readonly RabbitMqConnection connection;
    private readonly ILogger<BoardEventPublisher> logger;

    /// <summary>
    /// Serialises publishing. Channels are not thread-safe, and two requests finishing at the same
    /// moment would otherwise use this one concurrently.
    /// </summary>
    private readonly SemaphoreSlim gate = new(1, 1);

    private IChannel? channel;

    public BoardEventPublisher(RabbitMqConnection connection, ILogger<BoardEventPublisher> logger)
    {
        this.connection = connection;
        this.logger = logger;
    }

    public async Task PublishAsync(int boardId, string reason, CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);

        try
        {
            var publishChannel = await EnsureChannelAsync(cancellationToken);

            var message = new BoardChangedEvent(Guid.NewGuid(), boardId, reason);

            var properties = new BasicProperties
            {
                // Written to disk by the broker, so a restart does not lose what is still queued.
                Persistent = true,
                ContentType = "application/json",
                MessageId = message.EventId.ToString()
            };

            await publishChannel.BasicPublishAsync(
                exchange: RealtimeTopology.ExchangeName,
                routingKey: RealtimeTopology.BoardChangedKey,
                mandatory: false,
                basicProperties: properties,
                body: JsonSerializer.SerializeToUtf8Bytes(message),
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "Published {Reason} for board {BoardId}. Event {EventId}.",
                reason,
                boardId,
                message.EventId);
        }
        catch (Exception exception) when (IsBrokerUnavailable(exception))
        {
            // The channel is likely unusable now; the next publish opens a fresh one.
            await DiscardChannelAsync();

            logger.LogWarning(
                exception,
                "RabbitMQ was unreachable, so {Reason} for board {BoardId} was not announced. The change itself was saved.",
                reason,
                boardId);
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
        catch
        {
            // Already broken, which is why it is being thrown away.
        }

        channel = null;
    }

    /// <summary>
    /// Narrow on purpose: a broker that is down is expected and survivable, but a serialisation
    /// defect or a bug here must keep travelling to the exception middleware.
    /// </summary>
    private static bool IsBrokerUnavailable(Exception exception)
        => exception is BrokerUnreachableException
            or AlreadyClosedException
            or OperationInterruptedException
            or ChannelAllocationException
            or TimeoutException
            or IOException;
}
