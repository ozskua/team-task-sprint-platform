namespace SprintFlow.Web.Server.Options;

/// <summary>
/// Token settings bound from the <c>Jwt</c> configuration section.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// HS256 signing key. Never committed: it lives in <c>appsettings.Development.json</c> locally
    /// and in an environment variable in production.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    /// <summary>Token lifetime in minutes. 480 = 8 hours; there is no refresh token.</summary>
    public int ExpiryMinutes { get; set; }
}
