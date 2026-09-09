using SprintFlow.Web.Server.Common.Enums;

namespace SprintFlow.Web.Server.DTOs.Notifications;

// No message text: the sentence is composed on the client from the type and the two names below,
// so a task renamed afterwards is not quoted by its old title. BoardId is what the client
// navigates to when the notification is clicked.
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
