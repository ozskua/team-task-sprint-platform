using Microsoft.EntityFrameworkCore;
using NLog;
using NLog.Extensions.Logging;
using SprintFlow.Web.Notification.Data;
using SprintFlow.Web.Notification.Messaging;
using SprintFlow.Web.Notification.Options;
using SprintFlow.Web.Notification.Services;

// Loaded before the host so that failures during start-up are still written to the log file.
var logger = LogManager.Setup()
    .LoadConfigurationFromFile("nlog.config")
    .GetCurrentClassLogger();

try
{
    logger.Info("Notification service starting.");

    // A plain generic host, not a web host: this service has no endpoints, so it must not open a
    // listening port. That is not only tidiness — binding one is a real liability here. The
    // scaffolded port 5086 turned out to sit inside a Hyper-V/WSL reserved range (5041-5140), so
    // Kestrel could not bind it and the whole service died on start with SocketException 10013.
    // Those ranges move when Docker restarts, so any port would be a standing risk. Owning none
    // removes the failure class entirely.
    var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
    {
        Args = args,

        // The generic host reads DOTNET_ENVIRONMENT, while the other two services - and every
        // launch profile, including this project's - set ASPNETCORE_ENVIRONMENT. Honouring both
        // means appsettings.Development.json loads the same way here as everywhere else, however
        // the service is started.
        EnvironmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
    });

    builder.Logging.ClearProviders();
    builder.Logging.AddNLog();

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    // Fail fast rather than run misconfigured. Without a database this service can consume messages
    // but never keep them, and the symptom - notifications that quietly never appear - is one of the
    // hardest to diagnose from the outside.
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "Connection string 'DefaultConnection' is not configured. Set it in "
            + "appsettings.Development.json, which is git-ignored, pointing at the same database the "
            + "API service owns.");
    }

    // Write-only, and it owns no schema: the API service holds every migration. See
    // NotificationWriteDbContext.
    builder.Services.AddDbContext<NotificationWriteDbContext>(options =>
        options.UseSqlServer(
            connectionString,
            sql => sql.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null)));

    builder.Services.Configure<RabbitMqOptions>(
        builder.Configuration.GetSection(RabbitMqOptions.SectionName));

    // One connection for the process; the consumer and the publisher each open their own channel.
    builder.Services.AddSingleton<RabbitMqConnection>();
    builder.Services.AddSingleton<DeliveryEventPublisher>();

    // Scoped, because it sits on top of the scoped DbContext. The consumer creates a scope per
    // message to reach it.
    builder.Services.AddScoped<NotificationWriter>();

    builder.Services.AddHostedService<NotificationConsumer>();

    var host = builder.Build();

    // No endpoints, no authentication, no SignalR, and no listening socket. This service accepts
    // nothing from a browser: it reads one queue, writes rows, and announces them on another.

    logger.Info("Notification service started in {0} environment.", builder.Environment.EnvironmentName);

    host.Run();

    logger.Info("Notification service shutting down.");
}
catch (Exception exception)
{
    logger.Error(exception, "Notification service terminated unexpectedly.");
    throw;
}
finally
{
    LogManager.Shutdown();
}
