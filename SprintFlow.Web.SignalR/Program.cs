using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using NLog;
using NLog.Web;
using SprintFlow.Web.SignalR.Hubs;
using SprintFlow.Web.SignalR.Messaging;
using SprintFlow.Web.SignalR.Options;

// Loaded before the host so that failures during start-up are still written to the log file.
var logger = LogManager.Setup()
    .LoadConfigurationFromFile("nlog.config")
    .GetCurrentClassLogger();

try
{
    logger.Info("Realtime service starting.");

    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    builder.Services.Configure<RabbitMqOptions>(
        builder.Configuration.GetSection(RabbitMqOptions.SectionName));

    var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
        ?? new JwtOptions();

    // Fail fast rather than run misconfigured, the same way the API service does. An empty key
    // would make every token invalid and the symptom - "realtime silently never works" - is one of
    // the hardest to diagnose from the outside.
    if (string.IsNullOrWhiteSpace(jwt.Key))
    {
        throw new InvalidOperationException(
            "'Jwt:Key' is not configured. It is deliberately absent from every committed file: "
            + "set it as the environment variable Jwt__Key, or with 'dotnet user-secrets', using the "
            + "same value the API service issues tokens with.");
    }

    // Validation only. This service holds no ITokenService and has no sign-in endpoint: issuing
    // tokens stays with the API service. Note that the key is symmetric, so this is a property of
    // the code rather than of the cryptography - see the roadmap's RS256 entry.
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(bearer =>
        {
            bearer.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwt.Issuer,

                ValidateAudience = true,
                ValidAudience = jwt.Audience,

                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),

                ClockSkew = TimeSpan.Zero,

                RoleClaimType = System.Security.Claims.ClaimTypes.Role,

                // What SignalR's default provider reads to answer "who is this connection?", which
                // is what makes Clients.User(id) reach the right person. The API service writes
                // this claim into every token it issues.
                NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier
            };

            bearer.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    // A WebSocket handshake cannot carry an Authorization header, so the SignalR
                    // client sends the token in the query string instead.
                    var accessToken = context.Request.Query["access_token"];

                    if (!string.IsNullOrEmpty(accessToken)
                        && context.Request.Path.StartsWithSegments("/hubs"))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization();

    builder.Services.AddSignalR();

    // One connection for the process; the consumer opens its own channel on it.
    builder.Services.AddSingleton<RabbitMqConnection>();
    builder.Services.AddHostedService<RealtimeConsumer>();

    var app = builder.Build();

    app.UseAuthentication();
    app.UseAuthorization();

    // The same two paths the browser has always used. Only the process answering them changed,
    // which is why no Angular file had to.
    app.MapHub<BoardHub>("/hubs/board");
    app.MapHub<NotificationHub>("/hubs/notifications");

    logger.Info("Realtime service started in {0} environment.", app.Environment.EnvironmentName);

    app.Run();

    logger.Info("Realtime service shutting down.");
}
catch (Exception exception)
{
    logger.Error(exception, "Realtime service terminated unexpectedly.");
    throw;
}
finally
{
    LogManager.Shutdown();
}
