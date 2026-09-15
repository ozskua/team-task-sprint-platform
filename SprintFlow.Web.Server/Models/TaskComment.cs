namespace SprintFlow.Web.Server.Models;

public class TaskComment
{
    public int TaskCommentId { get; set; }

    public int TaskId { get; set; }

    public int UserId { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; }

    public TaskItem Task { get; set; } = null!;

    public User User { get; set; } = null!;

    public ICollection<TaskAttachment> Attachments { get; set; } = new List<TaskAttachment>();
}
