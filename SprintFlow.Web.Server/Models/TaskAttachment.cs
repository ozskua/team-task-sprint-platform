namespace SprintFlow.Web.Server.Models;

public class TaskAttachment
{
    public int TaskAttachmentId { get; set; }

    public int TaskId { get; set; }

    public int? TaskCommentId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string StoredName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public int UploadedByUserId { get; set; }

    public DateTime CreatedDate { get; set; }

    public TaskItem Task { get; set; } = null!;

    public TaskComment? Comment { get; set; }

    public User UploadedByUser { get; set; } = null!;
}
