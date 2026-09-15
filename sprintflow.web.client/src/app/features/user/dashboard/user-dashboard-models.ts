export type TaskStatus = 'Todo' | 'InProgress' | 'Completed';

export interface UserTask {
  readonly taskId: number;
  readonly title: string;
  readonly teamName: string;
  readonly status: TaskStatus;
  readonly dueDate: string | null;
}

export interface UserTeam {
  readonly teamId: number;
  readonly teamName: string;
  readonly memberCount: number;
}

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
