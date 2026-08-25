namespace SprintFlow.Web.Server.Common.Enums;

/// <summary>
/// What happened when a task was created. The service reports the outcome; turning it into a
/// status code is the controller's job, the same split the rest of the project uses.
/// </summary>
public enum CreateTaskOutcome
{
    Created,

    /// <summary>No board with the requested id — 404.</summary>
    BoardNotFound,

    /// <summary>A column id was supplied but no such column exists — 400.</summary>
    ColumnNotFound,

    /// <summary>The supplied column belongs to a different board — 400, and nothing is created.</summary>
    ColumnOnDifferentBoard,

    /// <summary>
    /// The named assignee does not exist or is no longer active — 400, and nothing is created. A
    /// disabled account cannot sign in, so making it answerable for a task would be a dead end.
    /// </summary>
    AssigneeUnavailable,

    /// <summary>A sprint id was supplied but no such sprint exists — 400.</summary>
    SprintNotFound,

    /// <summary>The supplied sprint belongs to a different board — 400, and nothing is created.</summary>
    SprintOnDifferentBoard
}
