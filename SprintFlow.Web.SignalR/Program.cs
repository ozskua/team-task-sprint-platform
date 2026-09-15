using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using NLog;
using NLog.Web;
using SprintFlow.Web.SignalR.Hubs;
using SprintFlow.Web.SignalR.Messaging;
using SprintFlow.Web.SignalR.Options;

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

    if (string.IsNullOrWhiteSpace(jwt.Key))
    {
        throw new InvalidOperationException(
            "'Jwt:Key' is not configured. It is deliberately absent from every committed file: "
            + "set it as the environment variable Jwt__Key, or with 'dotnet user-secrets', using the "
            + "same value the API service issues tokens with.");
    }

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

                NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier
            };

            bearer.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
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

    builder.Services.AddSingleton<RabbitMqConnection>();
    builder.Services.AddHostedService<RealtimeConsumer>();

    var app = builder.Build();

    app.UseAuthentication();
    app.UseAuthorization();

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
