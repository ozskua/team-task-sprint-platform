import { SystemColumnKind, TaskAssignee } from './board-detail-models';

export interface AdminDashboardCounts {
  readonly toDo: number;
  readonly open: number;
  readonly completed: number;
  readonly cancelled: number;
  readonly totalUsers: number;
  readonly totalBoards: number;
}

export interface AdminDashboardTask {
  readonly taskId: number;
  readonly title: string;
  readonly boardId: number;
  readonly boardName: string;
  readonly areaId: number;
  readonly areaName: string;
  readonly columnId: number;
  readonly columnName: string;
  readonly systemKind: SystemColumnKind | null;
  readonly createdByFullName: string;
  readonly createdDate: string;
  readonly assignee: TaskAssignee | null;
}

export interface AdminDashboard {
  readonly counts: AdminDashboardCounts;
  readonly tasks: readonly AdminDashboardTask[];
}
