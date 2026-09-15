namespace SprintFlow.Web.SignalR.Messaging;

public sealed record BoardChangedEvent(
    Guid EventId,
    int BoardId,
    string Reason);
