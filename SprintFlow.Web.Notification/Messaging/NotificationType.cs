namespace SprintFlow.Web.Notification.Messaging;

// This service's own copy of the contract's enum. The numbers are what travel in the message and
// what go into the database column, so they are explicit and must never be reordered.
//
// Declared here rather than shared through an assembly: see docs/EVENT_CONTRACTS.md, which is the
// authority if this and the API service's copy ever disagree.
public enum NotificationType
{
    TaskAssigned = 1,
    TaskCommented = 2,
    TaskCompleted = 3,
    TaskCancelled = 4
}
