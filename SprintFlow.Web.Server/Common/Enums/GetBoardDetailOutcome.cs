namespace SprintFlow.Web.Server.Common.Enums;

/// <summary>
/// What happened when the board page was read. It has more than one failure because the page can be
/// asked for a sprint as well as a board.
/// </summary>
public enum GetBoardDetailOutcome
{
    Found,

    /// <summary>No board with the requested id — 404.</summary>
    BoardNotFound,

    /// <summary>
    /// A sprint was named but it does not exist, or it belongs to another board — 400.
    /// </summary>
    /// <remarks>
    /// Reported rather than ignored. Filtering by a sprint the board does not have would return a
    /// board with no tasks on it, which looks like an empty board rather than a bad request.
    /// </remarks>
    SprintNotOnBoard,

    /// <summary>
    /// A sprint was named and every task was asked for at the same time — 400.
    /// </summary>
    /// <remarks>
    /// The two say opposite things about what to show, so there is no sensible reading of the
    /// request. Refused rather than silently letting one win.
    /// </remarks>
    ContradictoryScope
}
