using Microsoft.EntityFrameworkCore;
using NLog;
using NLog.Web;
using SprintFlow.Web.Server.Data;
using SprintFlow.Web.Server.Extensions;
using SprintFlow.Web.Server.Hubs;
using SprintFlow.Web.Server.Middleware;

// Loaded before the host so that failures during start-up are still written to the log file.
var logger = LogManager.Setup()
    .LoadConfigurationFromFile("nlog.config")
    .GetCurrentClassLogger();

try
{
    logger.Info("Application starting.");

    var builder = WebApplication.CreateBuilder(args);

    // Replaces the default providers with NLog rather than adding a second one alongside them.
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // Add services to the container.

    builder.Services.AddControllers();
    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();

    builder.Services.AddDbContext<SprintFlowDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sql => sql.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null)));

    builder.Services.AddAuthenticationServices(builder.Configuration);

    builder.Services.AddCachingServices(builder.Configuration);

    builder.Services.AddMessagingServices(builder.Configuration);

    builder.Services.AddApplicationServices(builder.Configuration);

    // Kept only for the TestHub mapped below, which is not part of the product. The board and
    // notification hubs moved to SprintFlow.Web.SignalR; if TestHub is ever removed, this can go
    // with it and this application will need no SignalR registration at all.
    builder.Services.AddSignalR();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowSpecificOrigins", policy =>
        {
            policy.WithOrigins(
                    "https://localhost:46230"
                )
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });

    var app = builder.Build();

    await app.ValidateAuthenticationConfigurationAsync(); // Fail fast rather than run misconfigured.

    app.UseMiddleware<ExceptionHandlingMiddleware>(); // First in the pipeline, so it catches everything after it.

    app.UseDefaultFiles();
    app.MapStaticAssets();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseHttpsRedirection();

    app.UseCors("AllowSpecificOrigins");

    app.UseAuthentication(); // Builds HttpContext.User from the incoming token.

    app.UseAuthorization(); // Checks whether that user may access the endpoint. Order matters.

    app.MapControllers();

    // Before the SPA fallback, which would otherwise answer the hub's own requests with index.html.
    // The board and notification hubs are no longer here: they are hosted by
    // SprintFlow.Web.SignalR, which owns every browser connection. This application publishes to
    // the broker and never talks to a browser over a socket.
    app.MapHub<TestHub>("/hubs/test"); //TestHub

    app.MapFallbackToFile("/index.html");

    logger.Info("Application started in {Environment} environment.", app.Environment.EnvironmentName);

    app.Run();

    logger.Info("Application shutting down.");
}
catch (Exception exception)
{
    // Start-up failures (bad configuration, unreachable database) would otherwise leave no trace.
    logger.Error(exception, "Application terminated unexpectedly.");
    throw;
}
finally
{
    // Flushes buffered messages and releases the file handle on every exit path.
    LogManager.Shutdown();
}
