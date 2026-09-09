using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using SprintFlow.Web.Server.Options;

namespace SprintFlow.Web.Server.Messaging;

/// <summary>
/// The application's single connection to RabbitMQ.
/// </summary>
/// <remarks>
/// <para>
/// A TCP connection and an AMQP handshake are expensive, and opening one per notification would be
/// the classic misuse of this client. One connection is shared by the publisher and the consumer,
/// which each open their own <b>channel</b> on it — that is the split the protocol is designed
/// around: connections are heavyweight and shareable, channels are cheap and are not thread-safe.
/// </para>
/// <para>
/// Opened lazily rather than at start-up, so the application starts and serves every request when
/// the broker is down — exactly as it does when Redis is unreachable. The lock makes the first
/// caller open it while the others wait, instead of racing to open several.
/// </para>
/// <para>
/// <c>AutomaticRecoveryEnabled</c> is the client's own reconnect: after the broker comes back it
/// reopens the connection, its channels and the consumer's subscription, so nothing here has to
/// poll or rebuild them.
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
    /// reached — the callers decide what that means for them.
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
                // Names this application in the management UI's connection list, which is what
                // makes that screen readable when something else is connected too.
                ClientProvidedName = "SprintFlow.Web.Server"
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
