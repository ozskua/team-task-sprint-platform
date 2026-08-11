namespace SprintFlow.Web.Server.Options;

/// <summary>
/// Authentication settings bound from the <c>Auth</c> configuration section.
/// </summary>
public class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>
    /// The user who reaches the Admin dashboard. The decision is a comparison against this value —
    /// never against an e-mail address and never against a role name. Kept in configuration so it
    /// can be changed without touching source.
    /// </summary>
    public int AdminUserId { get; set; }
}
