namespace SprintFlow.Web.Server.Common.Enums;

/// <summary>
/// What happened when a sprint was created. The service reports the outcome; turning it into a
/// status code is the controller's job, the same split the rest of the project uses.
/// </summary>
public enum CreateSprintOutcome
{
    Created,

    /// <summary>No board with the requested id — 404.</summary>
    BoardNotFound,

    /// <summary>
    /// The end day is not after the start day — 400. A sprint covering no days at all is not a
    /// sprint, so the two dates being equal is rejected here as well.
    /// </summary>
    EndNotAfterStart,

    /// <summary>
    /// Another sprint on this board already covers part of the same stretch of days — 400, and
    /// nothing is created. Sprints on one board form a single timeline.
    /// </summary>
    OverlapsExistingSprint
}
