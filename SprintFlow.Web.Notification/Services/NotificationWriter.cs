using SprintFlow.Web.Notification.Data;
using SprintFlow.Web.Notification.Messaging;

namespace SprintFlow.Web.Notification.Services;

/// <summary>
/// Turns one notification event into rows. Persistence only — announcing is a separate step.
/// </summary>
/// <remarks>
/// <b>The split into one method is the point.</b> Writing and announcing used to be a single call,
/// and a failure in the announcing half made the consumer requeue a message whose rows were already
/// committed, writing them a second time. Keeping the two apart lets the consumer acknowledge the
/// moment the rows are durable and treat a failed announcement as what it is: a lost live update,
/// not lost data.
/// </remarks>
public class NotificationWriter
{
    private readonly NotificationWriteDbContext context;

    public NotificationWriter(NotificationWriteDbContext context)
    {
        this.context = context;
    }

    /// <summary>
    /// Writes one row per recipient, in one save. Throws if the database refuses — the caller
    /// requeues on that, because it is worth trying again.
    /// </summary>
    /// <remarks>
    /// The recipients are taken exactly as the event gave them: already deduplicated, already
    /// without the actor, already decided at the moment the change happened. <c>CreatedDate</c> and
    /// <c>IsRead</c> are left to the database defaults.
    /// </remarks>
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
