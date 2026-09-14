/**
 * Shapes for the admin sprint report.
 *
 * Dates are plain `YYYY-MM-DD` strings, exactly as `sprint-models.ts` explains: these are whole
 * calendar days, and `endDate` is the first day *after* the sprint.
 */

import { SprintStatus } from './sprint-models';

/**
 * One sprint's outcome.
 *
 * The three percentages always total 100. Completed and Cancelled are rounded by the server and
 * Ongoing takes the remainder, so nothing here needs recomputing — and a sprint with no tasks is
 * zero everywhere rather than a division by zero.
 */
export interface SprintReportRow {
  readonly sprintId: number;
  readonly boardId: number;
  readonly boardName: string;

  /** Board names are not unique; the area is what tells two of them apart. */
  readonly areaId: number;
  readonly areaName: string;

  readonly name: string;
  readonly startDate: string;

  /** The first day after the sprint, as stored. Not part of it. */
  readonly endDate: string;

  readonly status: SprintStatus;

  readonly totalTasks: number;

  readonly completedCount: number;
  readonly completedPercent: number;

  readonly cancelledCount: number;
  readonly cancelledPercent: number;

  /** Everything that is neither completed nor cancelled: To Do and every custom column. */
  readonly ongoingCount: number;
  readonly ongoingPercent: number;
}
