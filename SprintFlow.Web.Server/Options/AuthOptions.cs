namespace SprintFlow.Web.Server.Options;

// AdminUserId is the whole admin decision: a comparison against this value, never against an
// e-mail address and never against a role name.
public class AuthOptions
{
    public const string SectionName = "Auth";

    public int AdminUserId { get; set; }
}
