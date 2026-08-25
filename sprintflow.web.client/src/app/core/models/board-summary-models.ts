/**
 * One board's own figures.
 *
 * Strictly board-scoped. The Dashboard's summary is a different thing — system-wide, over every
 * board — and the two never share a shape or a source.
 *
 * There is deliberately nothing about overdue or upcoming work: the schema holds no due date and no
 * completion timestamp, so those figures have nothing behind them and are absent rather than
 * reported as zero.
 */
export interface BoardSummary {
  readonly boardId: number;
  readonly boardName: string;

  /** The area the board sits in — board names are not unique. */
  readonly areaName: string;

  /** Every task on the board: `toDo + open + completed + cancelled`. */
  readonly totalTasks: number;

  readonly toDo: number;

  /** Tasks in any custom column — started, not finished. */
  readonly open: number;

  readonly completed: number;

  /** Counted in the total, excluded from progress. */
  readonly cancelled: number;

  /** `completed / (toDo + open + completed)`, rounded. Cancelled is not in the denominator. */
  readonly progressPercent: number;

  readonly tasksWithoutSprint: number;

  /** At most one, by the per-board non-overlap rule. */
  readonly activeSprints: number;
  readonly upcomingSprints: number;
  readonly archivedSprints: number;
}
