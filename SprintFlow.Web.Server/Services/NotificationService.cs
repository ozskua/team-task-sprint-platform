using SprintFlow.Web.Server.Data.Repositories;
using SprintFlow.Web.Server.DTOs.Notifications;
using SprintFlow.Web.Server.Mappings;

namespace SprintFlow.Web.Server.Services;

/// <summary>
/// Reading a user's notifications back. This application no longer writes them.
/// </summary>
/// <remarks>
/// Writing moved out with the consumer: the notification service takes the event off the queue and
/// inserts the rows, and the realtime service tells the recipients. What is left here is the read
/// side the bell's API needs — the queries that join a notification to its actor and its task,
/// which is why the rich entity and the schema stay in this application (see the roadmap on
/// database ownership).
/// </remarks>
public class NotificationService
{
    private readonly NotificationRepository notificationRepository;

    public NotificationService(NotificationRepository notificationRepository)
    {
        this.notificationRepository = notificationRepository;
    }

    // --- Reading ------------------------------------------------------------
    //
    // The user id every method here takes comes from the caller's validated token. No request DTO
    // models a recipient, so there is nothing a client could send to read someone else's rows.

    public async Task<IReadOnlyList<NotificationResponseDto>> GetForUserAsync(
        int userId,
        CancellationToken cancellationToken)
        => (await notificationRepository.GetForUserAsync(userId, cancellationToken)).ToResponseDtos();

    public Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken)
        => notificationRepository.CountUnreadAsync(userId, cancellationToken);

    /// <summary>
    /// Marks one notification read. <c>false</c> when this user has no such notification — which is
    /// the same answer for an id that does not exist and one that belongs to somebody else.
    /// </summary>
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

        // Already read is still success: the caller asked for a state, not for a change.
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
