using Microsoft.EntityFrameworkCore;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Data.Repositories;

/// <summary>
/// Reads and writes notifications. Every query here is filtered by the recipient, so a caller can
/// only ever reach their own rows — there is no method that returns a notification by id alone.
/// </summary>
public class NotificationRepository
{
    /// <summary>
    /// How many notifications the panel shows. There is no paging: the panel is a glance at what
    /// just happened, not an archive, and a fixed ceiling keeps the query's cost flat.
    /// </summary>
    private const int ListLimit = 30;

    private readonly SprintFlowDbContext context;

    public NotificationRepository(SprintFlowDbContext context)
    {
        this.context = context;
    }

    /// <summary>
    /// One user's most recent notifications, newest first.
    /// </summary>
    /// <remarks>
    /// Projected in SQL rather than loaded and mapped: only the ten values the panel draws leave
    /// the database, so neither the actor's password hash nor the task's description is ever read.
    /// </remarks>
    public async Task<IReadOnlyList<NotificationListItem>> GetForUserAsync(
        int userId,
        CancellationToken cancellationToken)
        => await context.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientUserId == userId)
            // Ties broken by id, so two notifications written in the same tick never swap places
            // between two reads.
            .OrderByDescending(n => n.CreatedDate)
            .ThenByDescending(n => n.NotificationId)
            .Take(ListLimit)
            .Select(n => new NotificationListItem(
                n.NotificationId,
                n.Type,
                n.ActorUserId,
                n.Actor.FirstName,
                n.Actor.LastName,
                n.TaskId,
                n.Task.Title,
                // The board is what the client navigates to, so it comes back with the row rather
                // than costing a second request per notification.
                n.Task.BoardId,
                n.IsRead,
                n.CreatedDate))
            .ToListAsync(cancellationToken);

    /// <summary>Counted by the database. No row is loaded to be counted in memory.</summary>
    public Task<int> CountUnreadAsync(int userId, CancellationToken cancellationToken)
        => context.Notifications
            .CountAsync(n => n.RecipientUserId == userId && !n.IsRead, cancellationToken);

    /// <summary>
    /// One notification, tracked so it can be marked read. Filtered by recipient as well as by id:
    /// somebody else's notification is simply not found, which is also why the API answers 404
    /// rather than 403 and tells a caller nothing about ids they do not own.
    /// </summary>
    public Task<Notification?> GetForUpdateAsync(
        int notificationId,
        int userId,
        CancellationToken cancellationToken)
        => context.Notifications
            .FirstOrDefaultAsync(
                n => n.NotificationId == notificationId && n.RecipientUserId == userId,
                cancellationToken);

    /// <summary>
    /// Marks everything this user has as read, in one UPDATE. Nothing is loaded, so a user with a
    /// long history costs the same as one with none.
    /// </summary>
    public Task<int> MarkAllReadAsync(int userId, CancellationToken cancellationToken)
        => context.Notifications
            .Where(n => n.RecipientUserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(n => n.IsRead, true),
                cancellationToken);

    /// <summary>Staged together, so one save writes every recipient's row.</summary>
    public void AddRange(IEnumerable<Notification> notifications)
        => context.Notifications.AddRange(notifications);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => context.SaveChangesAsync(cancellationToken);
}
