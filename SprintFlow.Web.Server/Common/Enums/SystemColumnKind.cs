namespace SprintFlow.Web.Server.Common.Enums;

// Explicit values, never the compiler's defaults: the numbers are stored in the database, so
// reordering the members would change what existing rows mean.
public enum SystemColumnKind
{
    ToDo = 1,
    Completed = 2,
    Cancelled = 3
}
