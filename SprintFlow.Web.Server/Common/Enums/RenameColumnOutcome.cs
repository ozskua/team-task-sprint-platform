namespace SprintFlow.Web.Server.Common.Enums;

/// <summary>What happened when a workflow column was renamed.</summary>
public enum RenameColumnOutcome
{
    Renamed,

    /// <summary>No column with the requested id — 404.</summary>
    ColumnNotFound,

    /// <summary>To Do, Completed and Cancelled can never be renamed — 400.</summary>
    ColumnIsSystem
}
