using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SprintFlow.Web.Server.Authentication;
using SprintFlow.Web.Server.Interfaces.Repositories;
using SprintFlow.Web.Server.Interfaces.Services;
using SprintFlow.Web.Server.Options;
using SprintFlow.Web.Server.Repositories;
using SprintFlow.Web.Server.Services;

namespace SprintFlow.Web.Server.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers everything sign-in needs: configuration binding, the auth collaborators, and the
    /// bearer scheme that will validate tokens once endpoints are protected.
    /// </summary>
    public static IServiceCollection AddAuthenticationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        // Stateless and thread-safe: one instance each.
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        // Scoped, because they sit on top of the scoped DbContext.
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAuthService, AuthService>();

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
    /// Feature services that are not part of signing in. <c>IUserRepository</c> is not registered
    /// again here — it is already registered above and serves both callers.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();

        services.AddScoped<IAreaRepository, AreaRepository>();
        services.AddScoped<IAreaService, AreaService>();

        services.AddScoped<IBoardRepository, BoardRepository>();
        services.AddScoped<IBoardService, BoardService>();
        services.AddScoped<IBoardDetailService, BoardDetailService>();

        services.AddScoped<IWorkflowColumnRepository, WorkflowColumnRepository>();
        services.AddScoped<IWorkflowColumnService, WorkflowColumnService>();

        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<ITaskService, TaskService>();

        services.AddScoped<ITaskParticipantRepository, TaskParticipantRepository>();

        services.AddScoped<ITeamRepository, TeamRepository>();
        services.AddScoped<ITeamService, TeamService>();

        // The system-wide view, over the task, user and board repositories. Separate from the one
        // above because the two answer different questions: "everything" versus "mine".
        services.AddScoped<IAdminDashboardService, AdminDashboardService>();

        return services;
    }
}
