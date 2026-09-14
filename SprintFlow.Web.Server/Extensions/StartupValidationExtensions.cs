using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SprintFlow.Web.Server.Common;
using SprintFlow.Web.Server.Data;
using SprintFlow.Web.Server.Options;

namespace SprintFlow.Web.Server.Extensions;

public static class StartupValidationExtensions
{
    /// <summary>
    /// Refuses to start when the authentication configuration is wrong. A bad Auth:AdminUserId
    /// crashes nothing — it just makes the wrong person admin, or nobody — so it is turned into a
    /// startup failure instead. Runs after the host is built because it queries the database.
    /// </summary>
    public static async Task ValidateAuthenticationConfigurationAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var provider = scope.ServiceProvider;

        var jwt = provider.GetRequiredService<IOptions<JwtOptions>>().Value;
        var auth = provider.GetRequiredService<IOptions<AuthOptions>>().Value;

        if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 32)
        {
            throw new InvalidOperationException(
                "Configuration 'Jwt:Key' is missing or shorter than 32 characters. It is deliberately "
                + "absent from every committed file: set it with 'dotnet user-secrets set \"Jwt:Key\" "
                + "<value> --project SprintFlow.Web.Server', or as the environment variable Jwt__Key. "
                + "The realtime service must be given the same value.");
        }

        if (string.IsNullOrWhiteSpace(jwt.Issuer) || string.IsNullOrWhiteSpace(jwt.Audience))
        {
            throw new InvalidOperationException("Configuration 'Jwt:Issuer' and 'Jwt:Audience' must both be set.");
        }

        if (jwt.ExpiryMinutes <= 0)
        {
            throw new InvalidOperationException("Configuration 'Jwt:ExpiryMinutes' must be greater than zero.");
        }

        if (auth.AdminUserId <= 0)
        {
            throw new InvalidOperationException(
                "Configuration 'Auth:AdminUserId' is missing or not a positive number.");
        }

        var invitations = provider.GetRequiredService<IOptions<InvitationOptions>>().Value;

        // Zero or negative would mint invitations that have already expired — every registration
        // link would fail, and it would look like a bug in registration rather than a setting.
        if (invitations.ExpiryHours <= 0)
        {
            throw new InvalidOperationException(
                "Configuration 'Invitations:ExpiryHours' must be greater than zero.");
        }

        var context = provider.GetRequiredService<SprintFlowDbContext>();

        var configuredAdmin = await context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == auth.AdminUserId);

        if (configuredAdmin is null)
        {
            throw new InvalidOperationException(
                $"Configuration 'Auth:AdminUserId' is {auth.AdminUserId}, but no user with that id exists. " +
                "Apply the migrations, or correct the configured id.");
        }

        if (configuredAdmin.Role.RoleName != RoleNames.Admin)
        {
            throw new InvalidOperationException(
                $"Configuration 'Auth:AdminUserId' is {auth.AdminUserId}, but that user's role is " +
                $"'{configuredAdmin.Role.RoleName}' rather than '{RoleNames.Admin}'.");
        }
    }
}
