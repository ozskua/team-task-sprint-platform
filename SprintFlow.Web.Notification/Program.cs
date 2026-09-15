using Microsoft.EntityFrameworkCore;
using NLog;
using NLog.Extensions.Logging;
using SprintFlow.Web.Notification.Data;
using SprintFlow.Web.Notification.Messaging;
using SprintFlow.Web.Notification.Options;
using SprintFlow.Web.Notification.Services;

var logger = LogManager.Setup()
    .LoadConfigurationFromFile("nlog.config")
    .GetCurrentClassLogger();

try
{
    logger.Info("Notification service starting.");

    var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
    {
        Args = args,

        EnvironmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
    });

    builder.Logging.ClearProviders();
    builder.Logging.AddNLog();

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "Connection string 'DefaultConnection' is not configured. Set it in "
            + "appsettings.Development.json, which is git-ignored, pointing at the same database the "
            + "API service owns.");
    }

    builder.Services.AddDbContext<NotificationWriteDbContext>(options =>
        options.UseSqlServer(
            connectionString,
            sql => sql.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null)));

    builder.Services.Configure<RabbitMqOptions>(
        builder.Configuration.GetSection(RabbitMqOptions.SectionName));

    builder.Services.AddSingleton<RabbitMqConnection>();
    builder.Services.AddSingleton<DeliveryEventPublisher>();

    builder.Services.AddScoped<NotificationWriter>();

    builder.Services.AddHostedService<NotificationConsumer>();

    var host = builder.Build();

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
