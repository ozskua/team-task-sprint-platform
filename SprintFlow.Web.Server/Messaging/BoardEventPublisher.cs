using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace SprintFlow.Web.Server.Messaging;

public class BoardEventPublisher
{
    private readonly RabbitMqConnection connection;
    private readonly ILogger<BoardEventPublisher> logger;

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
