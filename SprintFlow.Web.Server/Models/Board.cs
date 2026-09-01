namespace SprintFlow.Web.Server.Models;

public class Board
{
    public int BoardId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int AreaId { get; set; }

    public int CreatedByUserId { get; set; }

    public DateTime CreatedDate { get; set; }

    public Area Area { get; set; } = null!;

    public User CreatedByUser { get; set; } = null!;

    public Team Team { get; set; } = null!;

    public ICollection<WorkflowColumn> WorkflowColumns { get; set; } = new List<WorkflowColumn>();

    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();

    public ICollection<Sprint> Sprints { get; set; } = new List<Sprint>();
}
