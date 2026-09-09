using SprintFlow.Web.Server.Data.Repositories;
using SprintFlow.Web.Server.DTOs.Notifications;

namespace SprintFlow.Web.Server.Mappings;

/// <summary>
/// Hand-written projection to DTO mapping, kept in one place so what leaves the server is visible.
/// </summary>
public static class NotificationMappings
{
    /// <summary>
    /// The actor's name is composed here rather than in the query, for the same reason
    /// <c>User.FullName</c> is composed rather than stored — see <c>UserConfiguration</c>.
    /// </summary>
    public static NotificationResponseDto ToResponseDto(this NotificationListItem notification)
        => new(
            notification.NotificationId,
            notification.Type,
            notification.ActorUserId,
            $"{notification.ActorFirstName} {notification.ActorLastName}".Trim(),
            notification.TaskId,
            notification.TaskTitle,
            notification.BoardId,
            notification.IsRead,
            notification.CreatedDate);

    public static IReadOnlyList<NotificationResponseDto> ToResponseDtos(
        this IEnumerable<NotificationListItem> notifications)
        => notifications.Select(notification => notification.ToResponseDto()).ToList();
}
