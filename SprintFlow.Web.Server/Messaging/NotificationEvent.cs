using SprintFlow.Web.Server.Common.Enums;

namespace SprintFlow.Web.Server.Messaging;

public sealed record NotificationEvent(
    Guid EventId,
    NotificationType Type,
    int TaskId,
    int ActorUserId,
    IReadOnlyList<int> RecipientUserIds,
    DateTime CreatedAt);
