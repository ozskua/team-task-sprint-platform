namespace SprintFlow.Web.Server.Common.Enums;

/// <summary>What happened when a workflow column was deleted.</summary>
public enum DeleteColumnOutcome
{
    Deleted,

    /// <summary>No column with the requested id — 404.</summary>
    ColumnNotFound,

    /// <summary>To Do, Completed and Cancelled can never be deleted — 400.</summary>
    ColumnIsSystem,

    /// <summary>
    /// The column still holds tasks — 400. Nothing is moved, changed or deleted; the caller has to
    /// empty the column first.
    /// </summary>
    ColumnHasTasks
}
