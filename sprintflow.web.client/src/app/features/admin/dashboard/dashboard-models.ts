/**
 * Shapes only — no data. The dashboard renders whatever it is given; the values
 * arrive from the API once the backend exists.
 */

/** The seven admin metrics of FR-D1. Nothing else belongs here. */
export interface DashboardMetrics {
  readonly totalUsers: number;
  readonly totalTeams: number;
  readonly totalTasks: number;
  readonly todoTasks: number;
  readonly inProgressTasks: number;
  readonly completedTasks: number;
  readonly overdueTasks: number;
}

/** One day of the created-vs-completed trend. */
export interface TrendPoint {
  /** ISO date (yyyy-MM-dd). */
  readonly date: string;
  readonly created: number;
  readonly completed: number;
}

/** One row of the team overview table. */
export interface TeamRow {
  readonly teamId: number;
  readonly teamName: string;
  readonly memberCount: number;
  readonly openTasks: number;
  readonly completedTasks: number;
}

/** All counters at zero — the honest state before any data exists. */
export const EMPTY_METRICS: DashboardMetrics = {
  totalUsers: 0,
  totalTeams: 0,
  totalTasks: 0,
  todoTasks: 0,
  inProgressTasks: 0,
  completedTasks: 0,
  overdueTasks: 0
};
