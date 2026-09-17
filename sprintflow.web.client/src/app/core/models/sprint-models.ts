export type SprintStatus = 'Upcoming' | 'Active' | 'Archived';

export interface Sprint {
  readonly sprintId: number;
  readonly boardId: number;
  readonly name: string;

  readonly startDate: string;

  readonly endDate: string;

  readonly status: SprintStatus;

  readonly taskCount: number;

  readonly createdByUserId: number;
  readonly createdByFullName: string;

  readonly createdDate: string;
}

export interface SprintDetail extends Sprint {
  readonly boardName: string;
}

export interface SprintListItem {
  readonly sprintId: number;
  readonly boardId: number;
  readonly boardName: string;
  readonly areaId: number;
  readonly areaName: string;
  readonly name: string;
  readonly startDate: string;

  readonly endDate: string;

  readonly status: SprintStatus;
  readonly taskCount: number;
}

export interface SprintTask {
  readonly taskId: number;
  readonly title: string;

  readonly columnName: string;

  readonly systemKind: 'ToDo' | 'Completed' | 'Cancelled' | null;

  readonly assigneeUserId: number | null;
  readonly assigneeFullName: string | null;

  readonly createdByFullName: string;

  readonly createdDate: string;
}

export interface CreateSprintRequest {
  readonly name: string;
  readonly startDate: string;

  readonly endDate: string;
}
