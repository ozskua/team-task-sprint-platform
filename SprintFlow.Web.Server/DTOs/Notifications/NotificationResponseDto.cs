using SprintFlow.Web.Server.Common.Enums;

namespace SprintFlow.Web.Server.DTOs.Notifications;

public record NotificationResponseDto(
    int NotificationId,
    NotificationType Type,
    int ActorUserId,
    string ActorFullName,
    int TaskId,
    string TaskTitle,
    int BoardId,
    bool IsRead,
    DateTime CreatedDate);
