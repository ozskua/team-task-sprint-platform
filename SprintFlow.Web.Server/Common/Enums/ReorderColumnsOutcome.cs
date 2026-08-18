namespace SprintFlow.Web.Server.Common.Enums;

/// <summary>
/// What happened when a board's custom columns were reordered. Only the custom columns are
/// orderable: To Do stays first, Completed and Cancelled stay last, in that order.
/// </summary>
public enum ReorderColumnsOutcome
{
    Reordered,

    /// <summary>No board with the requested id — 404.</summary>
    BoardNotFound,

    /// <summary>The payload named a system column, which cannot be moved — 400.</summary>
    ContainsSystemColumn,

    /// <summary>
    /// The payload is not exactly the board's custom columns — something is missing, repeated, or
    /// belongs to another board — 400.
    /// </summary>
    ColumnSetMismatch
}
