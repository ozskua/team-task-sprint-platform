using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SprintFlow.Web.Server.Common.Constants;
using SprintFlow.Web.Server.Data;
using SprintFlow.Web.Server.Options;

namespace SprintFlow.Web.Server.Extensions;

public static class StartupValidationExtensions
{
    /// <summary>
    /// Refuses to start the application when the authentication configuration is wrong.
    /// </summary>
    /// <remarks>
    /// A bad <c>Auth:AdminUserId</c> does not crash anything — it just means nobody is an admin, or
    /// the wrong person is. That is the kind of fault that survives testing and shows up in a demo,
    /// so it is turned into a startup failure instead. There is deliberately no fallback to "some
    /// other admin": guessing would hide the misconfiguration.
    ///
    /// This runs after the host is built because it has to query the database.
    /// </remarks>
    public static async Task ValidateAuthenticationConfigurationAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var provider = scope.ServiceProvider;

        var jwt = provider.GetRequiredService<IOptions<JwtOptions>>().Value;
        var auth = provider.GetRequiredService<IOptions<AuthOptions>>().Value;

        if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 32)
        {
            throw new InvalidOperationException(
                "Configuration 'Jwt:Key' is missing or shorter than 32 characters. " +
                "Set it in appsettings.Development.json locally, or as an environment variable in production.");
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
