namespace SprintFlow.Web.Server.DTOs.Boards;

/// <summary>
/// One board's figures, for its Özet tab.
/// </summary>
/// <remarks>
/// Strictly scoped to the board named in the route. The Dashboard's summary is a different thing —
/// system-wide, over every board — and neither reads the other's numbers.
/// <para>
/// There is deliberately nothing about overdue or upcoming work: the schema holds no due date and
/// no completion timestamp, so those figures have nothing behind them and are absent rather than
/// reported as zero.
/// </para>
/// </remarks>
/// <param name="BoardId">The board these figures describe.</param>
/// <param name="BoardName">Its name, so the page needs no second call to title itself.</param>
/// <param name="AreaName">The area it sits in — board names are not unique.</param>
/// <param name="TotalTasks">
/// Every task on the board. Equal to <paramref name="ToDo"/> + <paramref name="Open"/> +
/// <paramref name="Completed"/> + <paramref name="Cancelled"/>, because a task sits in exactly one
/// column and every column falls into exactly one of those buckets.
/// </param>
/// <param name="ToDo">Tasks in the board's To Do column.</param>
/// <param name="Open">Tasks in any custom column — work started but not finished.</param>
/// <param name="Completed">Tasks in the Completed column.</param>
/// <param name="Cancelled">Tasks in the Cancelled column. Counted in the total, excluded from progress.</param>
/// <param name="ProgressPercent">
/// <c>Completed / (ToDo + Open + Completed)</c>, rounded. Cancelled is left out of the denominator:
/// abandoned work is neither outstanding nor delivered, and including it would let the figure be
/// raised by cancelling things. The same formula the Dashboard uses.
/// </param>
/// <param name="TasksWithoutSprint">Tasks on the board that are in no sprint at all.</param>
/// <param name="ActiveSprints">Sprints covering today. At most one, by the non-overlap rule.</param>
/// <param name="UpcomingSprints">Sprints that have not started.</param>
/// <param name="ArchivedSprints">Sprints that have finished.</param>
public record BoardSummaryResponseDto(
    int BoardId,
    string BoardName,
    string AreaName,
    int TotalTasks,
    int ToDo,
    int Open,
    int Completed,
    int Cancelled,
    int ProgressPercent,
    int TasksWithoutSprint,
    int ActiveSprints,
    int UpcomingSprints,
    int ArchivedSprints);
