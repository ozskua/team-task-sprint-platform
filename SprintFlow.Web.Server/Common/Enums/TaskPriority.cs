namespace SprintFlow.Web.Server.Common.Enums;

// Explicit values, never the compiler's defaults: the numbers are stored in the database, so
// reordering the members would change what existing rows mean. Normal is the column default, which
// is what existing tasks were backfilled with.
public enum TaskPriority
{
    Unprioritised = 0,
    Normal = 1,
    Important = 2,
    Critical = 3
}
