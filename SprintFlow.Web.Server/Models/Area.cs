namespace SprintFlow.Web.Server.Models;

public class Area
{
    public int AreaId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? WebhookUrl { get; set; }

    public int CreatedByUserId { get; set; }

    public DateTime CreatedDate { get; set; }

    public User CreatedByUser { get; set; } = null!;

    public ICollection<Board> Boards { get; set; } = new List<Board>();
}
