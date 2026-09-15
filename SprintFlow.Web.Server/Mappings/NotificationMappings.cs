using SprintFlow.Web.Server.Data.Repositories;
using SprintFlow.Web.Server.DTOs.Notifications;

namespace SprintFlow.Web.Server.Mappings;

public static class NotificationMappings
{
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
