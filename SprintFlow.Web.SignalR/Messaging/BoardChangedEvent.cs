namespace SprintFlow.Web.SignalR.Messaging;

/// <summary>
/// A board changed and everyone watching it should re-read it.
/// </summary>
/// <remarks>
/// The payload matches, field for field, what the API service used to hand the hub directly: a
/// board id and a reason. The reason is passed through untouched — <c>taskCreated</c>,
/// <c>taskUpdated</c>, <c>taskMoved</c>, <c>taskDeleted</c>, <c>taskSprintChanged</c> — because the
/// browser already receives exactly these strings and nothing about the client contract changed.
/// <para>
/// This service's own copy of the contract, for the same reason as
/// <see cref="NotificationDeliveredEvent"/>.
/// </para>
/// </remarks>
public sealed record BoardChangedEvent(
    Guid EventId,
    int BoardId,
    string Reason);
