namespace SprintFlow.Web.Server.Common.Enums;

/// <summary>What happened when a task's title, description or assignee was changed.</summary>
public enum UpdateTaskOutcome
{
    Updated,

    /// <summary>No task with the requested id — 404.</summary>
    TaskNotFound,

    /// <summary>
    /// The named assignee does not exist or is no longer active — 400, and nothing is changed.
    /// </summary>
    AssigneeUnavailable
}
