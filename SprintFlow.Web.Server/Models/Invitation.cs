namespace SprintFlow.Web.Server.Models;

public class Invitation
{
    public int InvitationId { get; set; }

    public byte[] TokenHash { get; set; } = [];

    public int InvitedByUserId { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? UsedAt { get; set; }

    public int? RegisteredUserId { get; set; }

    public User InvitedByUser { get; set; } = null!;

    public User? RegisteredUser { get; set; }
}
