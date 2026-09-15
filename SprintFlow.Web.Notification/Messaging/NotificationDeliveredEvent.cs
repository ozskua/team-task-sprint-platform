namespace SprintFlow.Web.Notification.Messaging;

public sealed record NotificationDeliveredEvent(
    Guid EventId,
    IReadOnlyList<int> RecipientUserIds);
