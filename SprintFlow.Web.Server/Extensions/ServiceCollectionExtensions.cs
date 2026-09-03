using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SprintFlow.Web.Server.Authentication;
using SprintFlow.Web.Server.Options;
using SprintFlow.Web.Server.Data.Repositories;
using SprintFlow.Web.Server.Services;
using StackExchange.Redis;

namespace SprintFlow.Web.Server.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>The configuration key holding the Redis server's address.</summary>
    private const string RedisConnectionStringName = "Redis";

    // Namespaces every key this application stores in Redis.
    private const string RedisInstanceName = "sprintflow:";

    // Applied to connecting AND to running a command. The client does not abort when it cannot
    // connect - it queues the command and waits - so a stopped Redis failed on the command timeout,
    // not the connect one: two five-second waits per summary was where ~11s came from. Half a
    // second is ~1000x what a local Redis needs, and giving up early is only a cache miss.
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

        // Stateless and thread-safe: one instance each.
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        // Scoped, because they sit on top of the scoped DbContext.
        services.AddScoped<UserRepository>();
        services.AddScoped<AuthService>();

        // Registration lives here rather than with the feature services: an invitation exists only
        // to create an account, so it belongs with the rest of signing in.
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
            // configuration, but how the client behaves belongs here. The connection string lives in
            // the git-ignored appsettings.Development.json, so timeouts written there would be lost.
            var redis = ConfigurationOptions.Parse(connectionString);

            redis.ConnectTimeout = RedisTimeoutMilliseconds;

            // The one that actually bounds a request when Redis is stopped. AsyncTimeout is what
            // the cache's own Get/Set run under; SyncTimeout is set to match so the two cannot
            // drift apart, since AsyncTimeout takes its default from it.
            redis.SyncTimeout = RedisTimeoutMilliseconds;
            redis.AsyncTimeout = RedisTimeoutMilliseconds;

            options.ConfigurationOptions = redis;

            // Prefixes every key this application writes, so a Redis instance shared with anything
            // else stays legible and nothing can collide. Cache keys themselves are built by the
            // classes that own them.
            options.InstanceName = RedisInstanceName;
        });

        // Singleton: it holds no per-request state and depends only on singletons — the same reason
        // IApplicationClock is one.
        services.AddSingleton<IBoardSummaryCache, BoardSummaryCache>();

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

        // Repositories: one per area of the schema rather than one per table.
        services.AddScoped<BoardRepository>();   // areas, boards, teams, columns
        services.AddScoped<TaskRepository>();    // tasks and their assignments
        services.AddScoped<SprintRepository>();

        services.AddScoped<BoardService>();
        services.AddScoped<WorkflowColumnService>();
        services.AddScoped<TaskService>();
        services.AddScoped<SprintService>();
        services.AddScoped<DashboardService>();

        // Singleton: it resolves the configured timezone once and then only reads the clock.
        services.AddSingleton<IApplicationClock, ApplicationClock>();

        // Singleton for the same reason: it resolves the attachment folder once and then only
        // reads and writes files in it.
        services.AddSingleton<AttachmentStorage>();

        return services;
    }
}
