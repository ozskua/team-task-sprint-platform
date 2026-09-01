using SprintFlow.Web.Server.Common.Enums;

namespace SprintFlow.Web.Server.Models;

public class WorkflowColumn
{
    public int WorkflowColumnId { get; set; }

    public int BoardId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Position { get; set; }

    public SystemColumnKind? SystemKind { get; set; }

    public DateTime CreatedDate { get; set; }

    public Board Board { get; set; } = null!;

    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
