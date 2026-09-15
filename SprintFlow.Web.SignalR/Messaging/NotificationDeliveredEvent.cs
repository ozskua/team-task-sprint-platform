namespace SprintFlow.Web.SignalR.Messaging;

public sealed record NotificationDeliveredEvent(
    Guid EventId,
    IReadOnlyList<int> RecipientUserIds);
