/**
 * Shapes only — no data. The dashboard renders whatever it is given; real values
 * arrive from the API once the backend exists.
 */

/** The three V1 statuses. No Draft, no priority, no extra workflow states. */
export type TaskStatus = 'Todo' | 'InProgress' | 'Completed';

/** A task the current user may see, i.e. one belonging to a team they are in. */
export interface UserTask {
  readonly taskId: number;
  readonly title: string;
  readonly teamName: string;
  readonly status: TaskStatus;
  /** ISO date (yyyy-MM-dd), or null when the task has no deadline. */
  readonly dueDate: string | null;
}

/** A team the current user belongs to. */
export interface UserTeam {
  readonly teamId: number;
  readonly teamName: string;
  readonly memberCount: number;
}

/** Counts across the user's own teams. */
export interface TaskCounts {
  readonly todo: number;
  readonly inProgress: number;
  readonly completed: number;
}

export const EMPTY_TASK_COUNTS: TaskCounts = {
  todo: 0,
  inProgress: 0,
  completed: 0
};
