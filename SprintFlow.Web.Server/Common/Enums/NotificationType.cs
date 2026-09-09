namespace SprintFlow.Web.Server.Common.Enums;

// Explicit values, never the compiler's defaults: the numbers are stored in the database, so
// reordering the members would change what existing rows mean. Nothing uses 0, so a row that
// somehow carries one is plainly wrong rather than quietly the first kind.
public enum NotificationType
{
    TaskAssigned = 1,
    TaskCommented = 2,
    TaskCompleted = 3,
    TaskCancelled = 4
}
