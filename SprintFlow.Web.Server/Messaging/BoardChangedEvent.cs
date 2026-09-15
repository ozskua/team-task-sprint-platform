namespace SprintFlow.Web.Server.Messaging;

public sealed record BoardChangedEvent(
    Guid EventId,
    int BoardId,
    string Reason);
