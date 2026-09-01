namespace SprintFlow.Web.Server.Models;

// Exactly one per board, created with it and never by hand. Membership is not stored here: it is
// computed from TaskParticipants.
public class Team
{
    public int TeamId { get; set; }

    public int BoardId { get; set; }

    public int CreatedByUserId { get; set; }

    public DateTime CreatedDate { get; set; }

    public Board Board { get; set; } = null!;

    public User CreatedByUser { get; set; } = null!;
}
