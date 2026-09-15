using SprintFlow.Web.Notification.Messaging;

namespace SprintFlow.Web.Notification.Data;

public class NotificationRow
{
    public int NotificationId { get; set; }

    public int RecipientUserId { get; set; }

    public int ActorUserId { get; set; }

    public NotificationType Type { get; set; }

    public int TaskId { get; set; }
}
