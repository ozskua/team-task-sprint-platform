namespace SprintFlow.Web.Server.Models;

public class Team
{
    public int TeamId { get; set; }

    public int BoardId { get; set; }

    public int CreatedByUserId { get; set; }

    public DateTime CreatedDate { get; set; }

    public Board Board { get; set; } = null!;

    public User CreatedByUser { get; set; } = null!;
}
