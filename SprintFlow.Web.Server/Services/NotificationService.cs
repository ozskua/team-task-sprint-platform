using SprintFlow.Web.Server.Data.Repositories;
using SprintFlow.Web.Server.DTOs.Notifications;
using SprintFlow.Web.Server.Mappings;

namespace SprintFlow.Web.Server.Services;

public class NotificationService
{
    private readonly NotificationRepository notificationRepository;

    public NotificationService(NotificationRepository notificationRepository)
    {
        this.notificationRepository = notificationRepository;
    }

public async Task<IReadOnlyList<NotificationResponseDto>> GetForUserAsync(
        int userId,
        CancellationToken cancellationToken)
        => (await notificationRepository.GetForUserAsync(userId, cancellationToken)).ToResponseDtos();

    public Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken)
        => notificationRepository.CountUnreadAsync(userId, cancellationToken);

    public async Task<bool> MarkReadAsync(
        int notificationId,
        int userId,
        CancellationToken cancellationToken)
    {
        var notification = await notificationRepository.GetForUpdateAsync(
            notificationId,
            userId,
            cancellationToken);

        if (notification is null)
        {
            return false;
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;

            await notificationRepository.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    public Task MarkAllReadAsync(int userId, CancellationToken cancellationToken)
        => notificationRepository.MarkAllReadAsync(userId, cancellationToken);
}
