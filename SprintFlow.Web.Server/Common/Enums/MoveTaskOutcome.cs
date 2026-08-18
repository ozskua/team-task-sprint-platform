namespace SprintFlow.Web.Server.Common.Enums;

/// <summary>What happened when a task was moved between or within columns.</summary>
/// <remarks>
/// There is deliberately no "column does not accept this task" member: a task may move into and out
/// of every column of its board, including Completed and Cancelled.
/// </remarks>
public enum MoveTaskOutcome
{
    Moved,

    /// <summary>No task with the requested id — 404.</summary>
    TaskNotFound,

    /// <summary>No column with the requested id — 400.</summary>
    ColumnNotFound,

    /// <summary>The target column belongs to a different board — 400, and nothing moves.</summary>
    ColumnOnDifferentBoard
}
