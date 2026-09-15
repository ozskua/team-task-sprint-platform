namespace SprintFlow.Web.Notification.Messaging;

public sealed record NotificationEvent(
    Guid EventId,
    NotificationType Type,
    int TaskId,
    int ActorUserId,
    IReadOnlyList<int> RecipientUserIds,
    DateTime CreatedAt);
