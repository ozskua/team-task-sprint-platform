using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using SprintFlow.Web.SignalR.Options;

namespace SprintFlow.Web.SignalR.Messaging;

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

    public async Task<IConnection> GetAsync(CancellationToken cancellationToken)
    {
        if (connection is { IsOpen: true })
        {
            return connection;
        }

        await gate.WaitAsync(cancellationToken);

        try
        {
            if (connection is { IsOpen: true })
            {
                return connection;
            }

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
                ClientProvidedName = "SprintFlow.Web.SignalR"
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
