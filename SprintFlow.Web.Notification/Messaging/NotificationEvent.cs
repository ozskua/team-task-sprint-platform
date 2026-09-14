namespace SprintFlow.Web.Notification.Messaging;

/// <summary>
/// One thing that happened to a task, and everyone who should hear about it.
/// </summary>
/// <remarks>
/// <para>
/// This service's own copy of the contract the API service publishes. Not shared through an
/// assembly: that would make the two projects compile and deploy together, which is the coupling
/// the split was for. <c>docs/EVENT_CONTRACTS.md</c> is the authority when the two are compared.
/// </para>
/// <para>
/// <b>The recipients arrive already decided.</b> They were worked out where the change happened, at
/// the moment it happened, with the actor already removed. This service does not recalculate them —
/// by the time it runs, the board's membership could differ, and the notification should describe
/// the board as it was when the thing happened.
/// </para>
/// </remarks>
public sealed record NotificationEvent(
    Guid EventId,
    NotificationType Type,
    int TaskId,
    int ActorUserId,
    IReadOnlyList<int> RecipientUserIds,
    DateTime CreatedAt);
