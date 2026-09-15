export interface DashboardMetrics {
  readonly totalUsers: number;
  readonly totalTeams: number;
  readonly totalTasks: number;
  readonly todoTasks: number;
  readonly inProgressTasks: number;
  readonly completedTasks: number;
  readonly overdueTasks: number;
}

export interface TrendPoint {
  readonly date: string;
  readonly created: number;
  readonly completed: number;
}

export interface TeamRow {
  readonly teamId: number;
  readonly teamName: string;
  readonly memberCount: number;
  readonly openTasks: number;
  readonly completedTasks: number;
}

export const EMPTY_METRICS: DashboardMetrics = {
  totalUsers: 0,
  totalTeams: 0,
  totalTasks: 0,
  todoTasks: 0,
  inProgressTasks: 0,
  completedTasks: 0,
  overdueTasks: 0
};
