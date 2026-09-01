namespace SprintFlow.Web.Server.Models;

public class Invitation
{
    public int InvitationId { get; set; }

    // SHA-256 of the token handed out; the token itself is shown once and never stored. varbinary
    // rather than a Base64 string because the database collates Turkish_CI_AS - case-INsensitive -
    // under which two different digests could compare equal on the unique index.
    public byte[] TokenHash { get; set; } = [];

    public int InvitedByUserId { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? UsedAt { get; set; }

    // Both ends of "who invited whom" live here rather than on User, which stays unchanged.
    public int? RegisteredUserId { get; set; }

    public User InvitedByUser { get; set; } = null!;

    public User? RegisteredUser { get; set; }
}
