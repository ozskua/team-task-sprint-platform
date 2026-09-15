using Microsoft.EntityFrameworkCore;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Data.Repositories;

public class NotificationRepository
{
    private const int ListLimit = 30;

    private readonly SprintFlowDbContext context;

    public NotificationRepository(SprintFlowDbContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyList<NotificationListItem>> GetForUserAsync(
        int userId,
        CancellationToken cancellationToken)
        => await context.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientUserId == userId)
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
                n.Task.BoardId,
                n.IsRead,
                n.CreatedDate))
            .ToListAsync(cancellationToken);

    public Task<int> CountUnreadAsync(int userId, CancellationToken cancellationToken)
        => context.Notifications
            .CountAsync(n => n.RecipientUserId == userId && !n.IsRead, cancellationToken);

    public Task<Notification?> GetForUpdateAsync(
        int notificationId,
        int userId,
        CancellationToken cancellationToken)
        => context.Notifications
            .FirstOrDefaultAsync(
                n => n.NotificationId == notificationId && n.RecipientUserId == userId,
                cancellationToken);

    public Task<int> MarkAllReadAsync(int userId, CancellationToken cancellationToken)
        => context.Notifications
            .Where(n => n.RecipientUserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(n => n.IsRead, true),
                cancellationToken);

    public void AddRange(IEnumerable<Notification> notifications)
        => context.Notifications.AddRange(notifications);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => context.SaveChangesAsync(cancellationToken);
}
