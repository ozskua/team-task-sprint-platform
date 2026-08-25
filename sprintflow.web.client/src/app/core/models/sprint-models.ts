/**
 * Shapes for the sprint endpoints.
 *
 * Dates are plain `YYYY-MM-DD` strings and never `Date` objects. A `Date` is a moment in time and
 * carries a timezone; these are whole calendar days, and constructing one would let the browser
 * shift the day. The server stores them as SQL `date` for the same reason.
 */

/** Where a sprint sits relative to today. Decided by the server, never worked out here. */
export type SprintStatus = 'Upcoming' | 'Active' | 'Archived';

export interface Sprint {
  readonly sprintId: number;
  readonly boardId: number;
  readonly name: string;

  /** First day of the sprint, and part of it. `YYYY-MM-DD`. */
  readonly startDate: string;

  /**
   * The day the sprint stops, and **not** part of it. `YYYY-MM-DD`.
   *
   * Exactly what the server stores — the interval is half-open `[startDate, endDate)`, so the last
   * day worked is the day before this one and the next sprint may start on it. Nothing converts
   * between the two forms any more: the field means the same thing everywhere.
   */
  readonly endDate: string;

  readonly status: SprintStatus;

  /** How many tasks are in this sprint. */
  readonly taskCount: number;

  /** Recorded ownership. It grants the creator no permission over the sprint. */
  readonly createdByUserId: number;
  readonly createdByFullName: string;

  /** When the sprint was created — an audit timestamp, not one of its days. */
  readonly createdDate: string;
}

/** A sprint's own page. Adds the board's name, since the page can be opened directly by URL. */
export interface SprintDetail extends Sprint {
  readonly boardName: string;
}

/**
 * A row of the system-wide sprint overview.
 *
 * Carries the board and its area because that page spans every board and groups by them — and two
 * boards may share a name, so the area is what tells them apart.
 *
 * Like `Sprint`, `endDate` is exclusive — the first day after the sprint.
 */
export interface SprintListItem {
  readonly sprintId: number;
  readonly boardId: number;
  readonly boardName: string;
  readonly areaId: number;
  readonly areaName: string;
  readonly name: string;
  readonly startDate: string;

  /** The first day after the sprint, as stored. Not part of it. */
  readonly endDate: string;

  readonly status: SprintStatus;
  readonly taskCount: number;
}

/**
 * One task in a sprint, as the task table renders it.
 *
 * The same fields the Backlog's rows show, minus anything only the Backlog can act on — this shape
 * is used by the read-only tables on the sprint detail page and the global Sprints page, and the
 * board is already known from context in both.
 */
export interface SprintTask {
  readonly taskId: number;
  readonly title: string;

  /** The column the task sits in — its status, in the board's own vocabulary. */
  readonly columnName: string;

  /** `null` for a custom column; drives the status chip's tint, as elsewhere. */
  readonly systemKind: 'ToDo' | 'Completed' | 'Cancelled' | null;

  /** `null` when nobody is responsible for it. */
  readonly assigneeFullName: string | null;

  readonly createdByFullName: string;

  /** ISO date-time without an offset, exactly as the server stored it. */
  readonly createdDate: string;
}

/** What the create form collects, exactly as the API takes it — no conversion in between. */
export interface CreateSprintRequest {
  readonly name: string;
  readonly startDate: string;

  /** The first day *after* the sprint. Must be later than `startDate`. */
  readonly endDate: string;
}
