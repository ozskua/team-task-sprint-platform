using SprintFlow.Web.Server.Common.Enums;

namespace SprintFlow.Web.Server.Messaging;

/// <summary>
/// One thing that happened to a task, and everyone who should hear about it. Carried over RabbitMQ
/// as JSON.
/// </summary>
/// <remarks>
/// <para>
/// <b>The recipients travel with the event.</b> They are worked out where the change happened, at
/// the moment it happened, and the consumer takes them as given rather than asking who the team is
/// now. Otherwise a notification would describe the membership at delivery time instead of at event
/// time — someone who joined the board in between would be told about a comment written before they
/// arrived, and someone whose last task was reassigned would silently not be told about one written
/// while they were still on it.
/// </para>
/// <para>
/// The actor is already excluded from <paramref name="RecipientUserIds"/> by the publisher: whether
/// somebody should be told about their own action is a domain rule, and it stays next to the domain
/// change rather than being re-decided downstream.
/// </para>
/// <para>
/// Deliberately specific to notifications. There is no envelope, no base type and no generic
/// payload — a second kind of event would get its own contract, which is cheaper to read than a
/// mechanism built for one that does not exist yet.
/// </para>
/// </remarks>
/// <param name="EventId">
/// Identifies this occurrence. Delivery is at-least-once, so the same event can arrive twice; this
/// is what a duplicate check would key on. Nothing consumes it yet — see the limitation recorded in
/// the roadmap.
/// </param>
/// <param name="CreatedAt">
/// When the domain change happened, in UTC — not when it was delivered. The notification rows keep
/// their own database-generated timestamp; this one is for reading a message in the management UI.
/// </param>
public sealed record NotificationEvent(
    Guid EventId,
    NotificationType Type,
    int TaskId,
    int ActorUserId,
    IReadOnlyList<int> RecipientUserIds,
    DateTime CreatedAt);
