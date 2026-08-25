namespace SprintFlow.Web.Server.Common.Enums;

/// <summary>
/// What happened when a task was put into a sprint, or taken out of one.
/// </summary>
public enum AssignSprintOutcome
{
    Assigned,

    /// <summary>No task with the requested id — 404.</summary>
    TaskNotFound,

    /// <summary>A sprint id was supplied but no such sprint exists — 400.</summary>
    SprintNotFound,

    /// <summary>
    /// The sprint belongs to a different board — 400, and nothing is changed. The composite foreign
    /// key would refuse it anyway; this check is what makes it a clear 400 rather than a 500.
    /// </summary>
    SprintOnDifferentBoard
}
