using SprintFlow.Web.Server.Common.Enums;

namespace SprintFlow.Web.Server.Models;

public class TaskItem
{
    public int TaskId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int BoardId { get; set; }

    public int TaskColumnId { get; set; }

    // Contiguous from zero within a column, and scoped to the board+column - never to a sprint.
    public int Position { get; set; }

    public TaskPriority Priority { get; set; } = TaskPriority.Normal;

    public int? SprintId { get; set; }

    public int CreatedByUserId { get; set; }

    public DateTime CreatedDate { get; set; }

    public Board Board { get; set; } = null!;

    public WorkflowColumn Column { get; set; } = null!;

    public Sprint? Sprint { get; set; }

    public User CreatedByUser { get; set; } = null!;

    public ICollection<TaskParticipant> Participants { get; set; } = new List<TaskParticipant>();

    public ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();

    public ICollection<TaskAttachment> Attachments { get; set; } = new List<TaskAttachment>();
}
