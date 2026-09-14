using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SprintFlow.Web.Server.Authentication;
using SprintFlow.Web.Server.Options;
using SprintFlow.Web.Server.Data.Repositories;
using SprintFlow.Web.Server.Messaging;
using SprintFlow.Web.Server.Services;
using StackExchange.Redis;

namespace SprintFlow.Web.Server.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>The configuration key holding the Redis server's address.</summary>
    private const string RedisConnectionStringName = "Redis";

    // Namespaces every key this application stores in Redis.
    private const string RedisInstanceName = "sprintflow:";

    // Applied to connecting AND to running a command: the client queues a command rather than
    // aborting, so a stopped Redis failed on the command timeout, not the connect one — two
    // five-second waits per summary. Half a second is far more than a local Redis needs, and giving
    // up early is only a cache miss.
    private const int RedisTimeoutMilliseconds = 500;

    /// <summary>
    /// Registers everything sign-in needs: configuration binding, the auth collaborators, and the
    /// bearer scheme that will validate tokens once endpoints are protected.
    /// </summary>
    public static IServiceCollection AddAuthenticationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ApplicationOptions>(configuration.GetSection(ApplicationOptions.SectionName));
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<InvitationOptions>(configuration.GetSection(InvitationOptions.SectionName));

        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        services.AddScoped<UserRepository>();
        services.AddScoped<AuthService>();

        // An invitation exists only to create an account, so it belongs with signing in.
        services.AddScoped<InvitationRepository>();
        services.AddScoped<InvitationService>();

        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        // Validates incoming tokens. Issuing them is JwtTokenService's job.
        services
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

                    ClockSkew = TimeSpan.Zero, // Default 5-minute tolerance would extend every expired token.

                    // Which claims [Authorize(Roles = ...)] and User.Identity.Name read.
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role,
                    NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier
                };

                bearer.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // A WebSocket handshake cannot carry an Authorization header, so the client
                        // sends the token in the query string. Hub paths only.
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

        services.AddAuthorization();

        return services;
    }

    /// <summary>
    /// The Redis-backed cache. Redis runs in Docker for development (docker-compose.yml at the
    /// solution root); the application itself is not containerised and reaches it over loopback.
    /// </summary>
    public static IServiceCollection AddCachingServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(RedisConnectionStringName);

        // A missing address stops start-up, like a missing JWT key. Redis being *unreachable* is a
        // different matter, handled where the cache is used - so Redis can start after the app.
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{RedisConnectionStringName}' is not configured. "
                + "Set it in appsettings.Development.json locally — '127.0.0.1:6379' matches the Redis service in docker-compose.yml. "
                + "Use the literal address rather than 'localhost': that name resolves to ::1 first, which nothing is listening on, "
                + "and the wasted attempt costs more than the connect timeout allows.");
        }

        services.AddStackExchangeRedisCache(options =>
        {
            // Parsed rather than assigned to options.Configuration: the address belongs in
            // configuration, but how the client behaves belongs here.
            var redis = ConfigurationOptions.Parse(connectionString);

            redis.ConnectTimeout = RedisTimeoutMilliseconds;

            // AsyncTimeout is what the cache's Get/Set run under and is what actually bounds a
            // request when Redis is stopped. SyncTimeout matches so the two cannot drift apart.
            redis.SyncTimeout = RedisTimeoutMilliseconds;
            redis.AsyncTimeout = RedisTimeoutMilliseconds;

            options.ConfigurationOptions = redis;

            // Prefixes every key, so a shared Redis instance stays legible and nothing collides.
            options.InstanceName = RedisInstanceName;
        });

        services.AddSingleton<IBoardSummaryCache, BoardSummaryCache>();
        services.AddSingleton<IAdminDashboardCache, AdminDashboardCache>();

        // Its only job is to keep the cache above warm: nothing invalidates that entry, so this
        // timer is the only thing that writes it.
        services.AddHostedService<AdminDashboardRefreshService>();

        return services;
    }

    /// <summary>
    /// RabbitMQ: the connection, the publisher and the consumer that turns events into
    /// notifications. Like Redis, the broker runs in Docker for development and the application
    /// itself is not containerised.
    /// </summary>
    public static IServiceCollection AddMessagingServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));

        // A missing host stops start-up, like a missing JWT key or Redis address. The broker being
        // *unreachable* is a different matter, handled where it is used - so RabbitMQ can start
        // after the application, and the application runs without it.
        if (string.IsNullOrWhiteSpace(configuration[$"{RabbitMqOptions.SectionName}:Host"]))
        {
            throw new InvalidOperationException(
                $"'{RabbitMqOptions.SectionName}:Host' is not configured. "
                + "Set it in appsettings.Development.json locally — '127.0.0.1' matches the RabbitMQ service in docker-compose.yml. "
                + "Use the literal address rather than 'localhost': that name resolves to ::1 first, which nothing is listening on.");
        }

        // Singletons: one connection for the application, one channel per publisher. Opening a
        // connection per message is the classic misuse of this client — see RabbitMqConnection.
        services.AddSingleton<RabbitMqConnection>();
        services.AddSingleton<NotificationEventPublisher>();
        services.AddSingleton<BoardEventPublisher>();

        // No consumer here: this application publishes and nothing else. The notification and
        // realtime services drain their own queues, each in its own process.

        return services;
    }

    /// <summary>
    /// Everything that is not signing in. <c>UserRepository</c> is registered above and serves both.
    /// </summary>
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AttachmentOptions>(configuration.GetSection(AttachmentOptions.SectionName));

        // A missing folder path stops start-up, like a missing JWT key or Redis address: uploads
        // would otherwise fail one at a time, long after the mistake was made.
        if (string.IsNullOrWhiteSpace(configuration[$"{AttachmentOptions.SectionName}:RootPath"]))
        {
            throw new InvalidOperationException(
                $"'{AttachmentOptions.SectionName}:RootPath' is not configured. "
                + "Set it in appsettings.Development.json locally — an absolute path to a folder outside "
                + "this repository, where task attachments are stored. The folder is created if missing.");
        }

        services.AddScoped<BoardRepository>();   // areas, boards, teams, columns
        services.AddScoped<TaskRepository>();    // tasks and their assignments
        services.AddScoped<SprintRepository>();
        services.AddScoped<NotificationRepository>();

        services.AddScoped<BoardService>();
        services.AddScoped<WorkflowColumnService>();
        services.AddScoped<TaskService>();
        services.AddScoped<SprintService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<NotificationService>();

        services.AddSingleton<IApplicationClock, ApplicationClock>();

        services.AddSingleton<AttachmentStorage>();

        return services;
    }
}
