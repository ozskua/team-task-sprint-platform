using SprintFlow.Web.Server.Common.Enums;

namespace SprintFlow.Web.Server.Data.Repositories;

public sealed record NotificationListItem(
    int NotificationId,
    NotificationType Type,
    int ActorUserId,
    string ActorFirstName,
    string ActorLastName,
    int TaskId,
    string TaskTitle,
    int BoardId,
    bool IsRead,
    DateTime CreatedDate);
