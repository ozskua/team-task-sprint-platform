import { SprintStatus } from './sprint-models';

export interface SprintReportRow {
  readonly sprintId: number;
  readonly boardId: number;
  readonly boardName: string;

  readonly areaId: number;
  readonly areaName: string;

  readonly name: string;
  readonly startDate: string;

  readonly endDate: string;

  readonly status: SprintStatus;

  readonly totalTasks: number;

  readonly completedCount: number;
  readonly completedPercent: number;

  readonly cancelledCount: number;
  readonly cancelledPercent: number;

  readonly ongoingCount: number;
  readonly ongoingPercent: number;
}

export type SprintReportExportFormat = 'xlsx' | 'pdf';

export interface SprintReportExportResult {
  readonly blob: Blob;
  readonly fileName: string;
  readonly delivered: number;
  readonly failed: number;
}
