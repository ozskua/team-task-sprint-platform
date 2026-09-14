namespace SprintFlow.Web.SignalR.Messaging;

/// <summary>
/// A notification has been written down, and these people should be told it exists.
/// </summary>
/// <remarks>
/// <para>
/// This service's own copy of the contract. It is deliberately <b>not</b> shared with the
/// notification service through an assembly: sharing the type would make the two projects compile
/// and deploy together, which is exactly the coupling the split was for. What they share is the
/// JSON, described in <c>docs/EVENT_CONTRACTS.md</c>.
/// </para>
/// <para>
/// The consequence is that the two declarations can drift. The guard against it is that a message
/// this service cannot make sense of is treated as a poison message, loudly, rather than as
/// something that half-works — see <see cref="RealtimeConsumer"/>.
/// </para>
/// <para>
/// Only what delivery needs: who to tell. The notification's text, type and task are read back over
/// REST by the client, so none of them travel here.
/// </para>
/// </remarks>
public sealed record NotificationDeliveredEvent(
    Guid EventId,
    IReadOnlyList<int> RecipientUserIds);
