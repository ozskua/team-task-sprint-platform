namespace SprintFlow.Web.Server.Models;

public class TaskComment
{
    public int TaskCommentId { get; set; }

    public int TaskId { get; set; }

    // Resolved from the caller's token, never from the request body.
    public int UserId { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; }

    public TaskItem Task { get; set; } = null!;

    public User User { get; set; } = null!;

    // The files posted with this comment. Restrict rather than Cascade, so the service removes them
    // itself - see TaskAttachmentConfiguration.
    public ICollection<TaskAttachment> Attachments { get; set; } = new List<TaskAttachment>();
}
