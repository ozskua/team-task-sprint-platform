namespace SprintFlow.Web.SignalR.Options;

/// <summary>
/// What this service needs to <b>validate</b> a token. It never issues one.
/// </summary>
/// <remarks>
/// Deliberately missing <c>ExpiryMinutes</c>, which the API service's copy has: that value only
/// matters when writing a token, and writing tokens is not this service's job. The absent property
/// is the design saying so.
/// <para>
/// <b>The key is never written into a committed file.</b> It arrives from the environment —
/// <c>Jwt__Key</c> — or from user secrets, and .NET configuration binds it here without any code
/// knowing where it came from. See <c>docs/EVENT_CONTRACTS.md</c> and the roadmap for the recorded
/// consequence of both services holding the same symmetric secret.
/// </para>
/// </remarks>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;
}
