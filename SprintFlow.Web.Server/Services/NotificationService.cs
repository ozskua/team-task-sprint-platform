using Microsoft.AspNetCore.SignalR;
using SprintFlow.Web.Server.Common.Enums;
using SprintFlow.Web.Server.Data.Repositories;
using SprintFlow.Web.Server.DTOs.Notifications;
using SprintFlow.Web.Server.Hubs;
using SprintFlow.Web.Server.Mappings;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Services;

/// <summary>
/// Writing notifications down and reading them back. Nothing here decides who receives one.
/// </summary>
/// <remarks>
/// This is the seam the broker was added around. <c>TaskService</c> works out the recipients and
/// publishes an event; <c>NotificationConsumer</c> takes it off the queue and calls
/// <see cref="CreateManyAsync"/>, which does exactly what the direct call did before RabbitMQ
/// existed. Nothing in this class knows a broker is involved — it has no queue, exchange or
/// connection, and no message-bus abstraction was introduced around it.
/// </remarks>
public class NotificationService
{
    private readonly NotificationRepository notificationRepository;
    private readonly IHubContext<NotificationHub> notificationHub;

    public NotificationService(
        NotificationRepository notificationRepository,
        IHubContext<NotificationHub> notificationHub)
    {
        this.notificationRepository = notificationRepository;
        this.notificationHub = notificationHub;
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

    // --- Writing ------------------------------------------------------------

    /// <summary>
    /// Writes one notification per recipient and tells each of them, in one save and one send.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Called by <c>NotificationConsumer</c> for each event taken off the queue. The recipients are
    /// taken as given: they were worked out where the change happened, at the moment it happened,
    /// and the actor was already removed there. Recalculating them here would describe the team as
    /// it is now rather than as it was when the thing happened.
    /// </para>
    /// <para>
    /// <c>CreatedDate</c> is intentionally not set; the database default supplies it, as everywhere
    /// else. The SignalR event carries no payload for the same reason the board's does not — the
    /// client re-reads over REST, which stays the source of truth. It is sent only after the rows
    /// are committed, so a client that reacts immediately finds them there.
    /// </para>
    /// </remarks>
    public async Task CreateManyAsync(
        IReadOnlyList<int> recipientIds,
        int taskId,
        NotificationType type,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        // A board whose only member is the person acting, or an unassigned task nobody else holds.
        if (recipientIds.Count == 0)
        {
            return;
        }

        notificationRepository.AddRange(recipientIds.Select(recipientId => new Notification
        {
            RecipientUserId = recipientId,
            ActorUserId = actorUserId,
            TaskId = taskId,
            Type = type
        }));

        await notificationRepository.SaveChangesAsync(cancellationToken);

        await notificationHub.Clients
            .Users(recipientIds.Select(recipientId => recipientId.ToString()).ToList())
            .SendAsync("notificationReceived", cancellationToken);
    }
}
