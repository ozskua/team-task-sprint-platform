namespace SprintFlow.Web.Server.Common.Enums;

/// <summary>
/// Where a sprint sits relative to today. Worked out from the sprint's dates every time it is
/// read — there is no status column, because storing it would mean something had to flip it.
/// </summary>
/// <remarks>
/// The interval is half-open: the start day counts, the end day does not. A sprint is therefore
/// <see cref="Archived"/> on its own <c>EndDate</c>, and <see cref="Active"/> on the day before.
/// <para>
/// No explicit values: unlike <see cref="SystemColumnKind"/> these numbers are never stored, so
/// nothing depends on them.
/// </para>
/// </remarks>
public enum SprintStatus
{
    /// <summary>Today is before the start day.</summary>
    Upcoming,

    /// <summary>Today is on or after the start day, and before the end day.</summary>
    Active,

    /// <summary>Today is on or after the end day.</summary>
    Archived
}
