namespace SprintFlow.Web.Server.Options;

// In configuration for the same reason Jwt:ExpiryMinutes is: a credential's lifetime is an
// operational decision.
public class InvitationOptions
{
    public const string SectionName = "Invitations";

    public int ExpiryHours { get; set; }
}
