namespace SprintFlow.Web.Server.Messaging;

/// <summary>
/// A board changed and everyone watching it should re-read it.
/// </summary>
/// <remarks>
/// <para>
/// The payload is exactly what this service used to hand the hub directly: a board id and a reason.
/// The reason strings are unchanged — <c>taskCreated</c>, <c>taskUpdated</c>, <c>taskMoved</c>,
/// <c>taskDeleted</c>, <c>taskSprintChanged</c> — because the browser already listens for these and
/// nothing about the client contract changed when the hub moved to another process.
/// </para>
/// <para>
/// This application's own copy of the contract; the realtime service declares its own. Sharing the
/// type through an assembly would tie the two builds together, which is the coupling the split was
/// for. <see cref="Guid"/> <c>EventId</c> identifies the occurrence for tracing, exactly as
/// <c>NotificationEvent</c> does.
/// </para>
/// </remarks>
public sealed record BoardChangedEvent(
    Guid EventId,
    int BoardId,
    string Reason);
