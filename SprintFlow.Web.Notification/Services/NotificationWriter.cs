using SprintFlow.Web.Notification.Data;
using SprintFlow.Web.Notification.Messaging;

namespace SprintFlow.Web.Notification.Services;

public class NotificationWriter
{
    private readonly NotificationWriteDbContext context;

    public NotificationWriter(NotificationWriteDbContext context)
    {
        this.context = context;
    }

    public async Task WriteAsync(
        IReadOnlyList<int> recipientUserIds,
        int taskId,
        NotificationType type,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        context.Notifications.AddRange(recipientUserIds.Select(recipientUserId => new NotificationRow
        {
            RecipientUserId = recipientUserId,
            ActorUserId = actorUserId,
            TaskId = taskId,
            Type = type
        }));

        await context.SaveChangesAsync(cancellationToken);
    }
}
