import { SystemColumnKind } from './board-detail-models';

export interface DashboardTaskCounts {
  readonly toDo: number;
  readonly open: number;
  readonly completed: number;
  readonly cancelled: number;
}

export interface DashboardTask {
  readonly taskId: number;
  readonly title: string;
  readonly boardId: number;
  readonly boardName: string;
  readonly areaId: number;
  readonly areaName: string;
  readonly columnId: number;
  readonly columnName: string;
  readonly systemKind: SystemColumnKind | null;
}

export interface DashboardTeam {
  readonly boardId: number;
  readonly boardName: string;
  readonly areaId: number;
  readonly areaName: string;
  readonly myAssignedTaskCount: number;
  readonly members: readonly DashboardTeamMember[];
}

export interface DashboardTeamMember {
  readonly userId: number;
  readonly fullName: string;
}

export interface Dashboard {
  readonly counts: DashboardTaskCounts;
  readonly myTasks: readonly DashboardTask[];
  readonly myTeams: readonly DashboardTeam[];
}
