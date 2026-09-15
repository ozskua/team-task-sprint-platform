using SprintFlow.Web.Server.Common.Enums;

namespace SprintFlow.Web.Server.Models;

public class Notification
{
    public int NotificationId { get; set; }

    public int RecipientUserId { get; set; }

    public int ActorUserId { get; set; }

    public NotificationType Type { get; set; }

    public int TaskId { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedDate { get; set; }

    public User Recipient { get; set; } = null!;

    public User Actor { get; set; } = null!;

    public TaskItem Task { get; set; } = null!;
}
