import { SystemColumnKind, TaskAssignee } from './board-detail-models';

/**
 * The admin overview: the state of the whole system, and every task in it.
 *
 * System-wide and identical for every caller — unlike the member dashboard, which answers "what is
 * mine". There is deliberately no user list here: the Users page already lists everyone, so only the
 * user *count* travels.
 */

/** The six summary figures, over every task, user and board. */
export interface AdminDashboardCounts {
  /** The system `To Do` column. */
  readonly toDo: number;
  /** Every custom column — Review, Testing, Bench, whatever a board defines. */
  readonly open: number;
  readonly completed: number;
  readonly cancelled: number;
  /** Every account, active and inactive. A count only — no user data is sent. */
  readonly totalUsers: number;
  readonly totalBoards: number;
}

/** One task, anywhere in the system. */
export interface AdminDashboardTask {
  readonly taskId: number;
  readonly title: string;
  /** Where the row links to. */
  readonly boardId: number;
  readonly boardName: string;
  readonly areaId: number;
  readonly areaName: string;
  readonly columnId: number;
  /** The column's own name, shown as-is — a board's vocabulary belongs to its people. */
  readonly columnName: string;
  /** `null` for a custom column. Decides the chip's tint and the status filter, never the label. */
  readonly systemKind: SystemColumnKind | null;
  /** Who wrote it down — not the same question as who owes it. */
  readonly createdByFullName: string;
  /** ISO date-time without an offset, exactly as the server stored it. */
  readonly createdDate: string;
  /** `null` when nobody is answerable for it yet. */
  readonly assignee: TaskAssignee | null;
}

export interface AdminDashboard {
  readonly counts: AdminDashboardCounts;
  /** Every task in every board, newest first. Completed and Cancelled included. */
  readonly tasks: readonly AdminDashboardTask[];
}
