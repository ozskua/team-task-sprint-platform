import { SystemColumnKind } from './board-detail-models';

/**
 * The signed-in user's own dashboard. Everything here is about the caller — the server works out
 * who that is from the token, so nothing in these shapes identifies a user.
 */

/**
 * How the user's assigned tasks are spread across the four kinds of column. Mutually exclusive and
 * exhaustive, so they always add up to the user's total assigned tasks.
 */
export interface DashboardTaskCounts {
  /** The system `To Do` column. */
  readonly toDo: number;
  /** Every custom column — Review, Testing, Blocked, whatever a board defines. */
  readonly open: number;
  readonly completed: number;
  readonly cancelled: number;
}

/** One task assigned to the user, carrying enough of its board to make sense away from it. */
export interface DashboardTask {
  readonly taskId: number;
  readonly title: string;
  /** Where the row links to. */
  readonly boardId: number;
  readonly boardName: string;
  readonly areaId: number;
  readonly areaName: string;
  readonly columnId: number;
  /** The column's own name, shown as-is — a board's vocabulary is the user's. */
  readonly columnName: string;
  /** `null` for a custom column. Decides the chip's tint, never the label. */
  readonly systemKind: SystemColumnKind | null;
}

/**
 * One team the user belongs to. A team has no name of its own — it exists because a board does,
 * one for one — so the board names it and the board is what the card links to.
 */
export interface DashboardTeam {
  readonly boardId: number;
  readonly boardName: string;
  readonly areaId: number;
  readonly areaName: string;
  /**
   * Every task assigned to the user on this board, in **any** column. Not filtered the way the
   * task list is, so it can be larger than the number of that board's rows under My Tasks.
   */
  readonly myAssignedTaskCount: number;
  /** Each person once, however many tasks they hold. The server deduplicates. */
  readonly members: readonly DashboardTeamMember[];
}

export interface DashboardTeamMember {
  readonly userId: number;
  readonly fullName: string;
}

export interface Dashboard {
  /** Over **all** assigned tasks, including the ones `myTasks` leaves out. */
  readonly counts: DashboardTaskCounts;
  /** The actionable ones only: To Do and custom columns. */
  readonly myTasks: readonly DashboardTask[];
  readonly myTeams: readonly DashboardTeam[];
}
