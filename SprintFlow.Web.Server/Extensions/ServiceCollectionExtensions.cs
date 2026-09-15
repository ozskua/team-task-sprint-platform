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
    private const string RedisConnectionStringName = "Redis";

    private const string RedisInstanceName = "sprintflow:";

    private const int RedisTimeoutMilliseconds = 500;

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

        services.AddScoped<InvitationRepository>();
        services.AddScoped<InvitationService>();

        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

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

        services.AddAuthorization();

        return services;
    }

    public static IServiceCollection AddCachingServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(RedisConnectionStringName);

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
            var redis = ConfigurationOptions.Parse(connectionString);

            redis.ConnectTimeout = RedisTimeoutMilliseconds;

            redis.SyncTimeout = RedisTimeoutMilliseconds;
            redis.AsyncTimeout = RedisTimeoutMilliseconds;

            options.ConfigurationOptions = redis;

            options.InstanceName = RedisInstanceName;
        });

        services.AddSingleton<IBoardSummaryCache, BoardSummaryCache>();
        services.AddSingleton<IAdminDashboardCache, AdminDashboardCache>();

        services.AddHostedService<AdminDashboardRefreshService>();

        return services;
    }

    public static IServiceCollection AddMessagingServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));

        if (string.IsNullOrWhiteSpace(configuration[$"{RabbitMqOptions.SectionName}:Host"]))
        {
            throw new InvalidOperationException(
                $"'{RabbitMqOptions.SectionName}:Host' is not configured. "
                + "Set it in appsettings.Development.json locally — '127.0.0.1' matches the RabbitMQ service in docker-compose.yml. "
                + "Use the literal address rather than 'localhost': that name resolves to ::1 first, which nothing is listening on.");
        }

        services.AddSingleton<RabbitMqConnection>();
        services.AddSingleton<NotificationEventPublisher>();
        services.AddSingleton<BoardEventPublisher>();

return services;
    }

    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AttachmentOptions>(configuration.GetSection(AttachmentOptions.SectionName));

        if (string.IsNullOrWhiteSpace(configuration[$"{AttachmentOptions.SectionName}:RootPath"]))
        {
            throw new InvalidOperationException(
                $"'{AttachmentOptions.SectionName}:RootPath' is not configured. "
                + "Set it in appsettings.Development.json locally — an absolute path to a folder outside "
                + "this repository, where task attachments are stored. The folder is created if missing.");
        }

        services.AddScoped<BoardRepository>();
        services.AddScoped<TaskRepository>();
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
