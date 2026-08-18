namespace SprintFlow.Web.Server.Models;

/// <summary>
/// The team belonging to one board. Created automatically with its Board and never by hand; a Team
/// is never shared between boards.
/// </summary>
/// <remarks>
/// <see cref="CreatedByUserId"/> is the ownership reference, inherited from whoever created the
/// Board. There is deliberately no separate owner field, and neither the reference nor the act of
/// creating a Board grants any permission.
/// </remarks>
public class Team
{
    public int TeamId { get; set; }

    /// <summary>The one board this team belongs to. Unique — see <c>TeamConfiguration</c>.</summary>
    public int BoardId { get; set; }

    /// <summary>Inherited from the Board's creator. Ownership information only.</summary>
    public int CreatedByUserId { get; set; }

    /// <summary>Supplied by the database default — see <c>TeamConfiguration</c>.</summary>
    public DateTime CreatedDate { get; set; }

    public Board Board { get; set; } = null!;

    public User CreatedByUser { get; set; } = null!;
}
