using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using SprintFlow.Web.Notification.Options;

namespace SprintFlow.Web.Notification.Messaging;

/// <summary>
/// This service's single connection to RabbitMQ.
/// </summary>
/// <remarks>
/// <para>
/// A TCP connection and an AMQP handshake are expensive. One connection is shared by everything in
/// this process, and the consumer opens its own <b>channel</b> on it — that is the split the
/// protocol is designed around: connections are heavyweight and shareable, channels are cheap and
/// are not thread-safe.
/// </para>
/// <para>
/// Opened lazily, so the service starts and accepts browser connections even when the broker is
/// down. Clients stay connected and simply receive nothing until the broker returns.
/// </para>
/// <para>
/// A copy of the API service's class rather than a shared one. It is infrastructure, not a
/// contract: the two processes have no reason to evolve their connection handling together, and
/// sharing it would tie their build and deployment to each other.
/// </para>
/// </remarks>
public class RabbitMqConnection : IAsyncDisposable
{
    private readonly RabbitMqOptions options;
    private readonly ILogger<RabbitMqConnection> logger;
    private readonly SemaphoreSlim gate = new(1, 1);

    private IConnection? connection;

    public RabbitMqConnection(IOptions<RabbitMqOptions> options, ILogger<RabbitMqConnection> logger)
    {
        this.options = options.Value;
        this.logger = logger;
    }

    /// <summary>
    /// The open connection, opening it first if necessary. Throws when the broker cannot be
    /// reached — the caller decides what that means.
    /// </summary>
    public async Task<IConnection> GetAsync(CancellationToken cancellationToken)
    {
        if (connection is { IsOpen: true })
        {
            return connection;
        }

        await gate.WaitAsync(cancellationToken);

        try
        {
            // Checked again inside the lock: another caller may have opened it while this one
            // waited, and opening a second connection would leak the first.
            if (connection is { IsOpen: true })
            {
                return connection;
            }

            // Closed rather than merely null when a previous connection died and could not recover.
            if (connection is not null)
            {
                await connection.DisposeAsync();
                connection = null;
            }

            var factory = new ConnectionFactory
            {
                HostName = options.Host,
                Port = options.Port,
                UserName = options.UserName,
                Password = options.Password,
                VirtualHost = options.VirtualHost,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                // Names this service in the management UI's connection list, which is what makes
                // that screen readable now that three processes share the broker.
                ClientProvidedName = "SprintFlow.Web.Notification"
            };

            connection = await factory.CreateConnectionAsync(cancellationToken);

            logger.LogInformation(
                "Connected to RabbitMQ at {Host}:{Port}.",
                options.Host,
                options.Port);

            return connection;
        }
        finally
        {
            gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (connection is not null)
        {
            await connection.DisposeAsync();
            connection = null;
        }

        gate.Dispose();

        GC.SuppressFinalize(this);
    }
}
