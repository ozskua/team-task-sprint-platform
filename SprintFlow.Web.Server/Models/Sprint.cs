namespace SprintFlow.Web.Server.Models;

public class Sprint
{
    public int SprintId { get; set; }

    public int BoardId { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public int CreatedByUserId { get; set; }

    public DateTime CreatedDate { get; set; }

    public Board Board { get; set; } = null!;

    public User CreatedByUser { get; set; } = null!;

    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
