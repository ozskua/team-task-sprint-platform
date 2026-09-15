export interface BoardSummary {
  readonly boardId: number;
  readonly boardName: string;

  readonly areaName: string;

  readonly totalTasks: number;

  readonly toDo: number;

  readonly open: number;

  readonly completed: number;

  readonly cancelled: number;

  readonly progressPercent: number;

  readonly tasksWithoutSprint: number;

  readonly activeSprints: number;
  readonly upcomingSprints: number;
  readonly archivedSprints: number;
}
