namespace SprintFlow.Web.Notification.Messaging;

/// <summary>
/// A notification has been written down, and these people should be told it exists.
/// </summary>
/// <remarks>
/// Published by this service once the rows are durable, and consumed by the realtime service, which
/// owns the browser connections. Only what delivery needs travels: who to tell. The notification's
/// text, type and task are read back over REST by the client, so none of them belong here.
/// <para>
/// This service's own copy, matching the realtime service's declaration field for field.
/// </para>
/// </remarks>
public sealed record NotificationDeliveredEvent(
    Guid EventId,
    IReadOnlyList<int> RecipientUserIds);
