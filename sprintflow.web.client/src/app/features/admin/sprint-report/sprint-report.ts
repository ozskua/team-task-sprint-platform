import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  signal
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { SprintTask } from '../../../core/models/sprint-models';
import { SprintReportRow } from '../../../core/models/sprint-report-models';
import { SprintReportService } from '../../../core/services/sprint-report.service';
import { SprintService } from '../../../core/services/sprint.service';
import { TaskTableRow } from '../../../shared/task-table/task-table';

/** What is known about one sprint's tasks, once someone has opened it. */
interface TaskState {
  readonly status: 'loading' | 'ready' | 'failed';
  readonly completed: readonly TaskTableRow[];
  readonly cancelled: readonly TaskTableRow[];
  readonly ongoing: readonly TaskTableRow[];
}

const NO_TASKS: TaskState = { status: 'loading', completed: [], cancelled: [], ongoing: [] };

/** The four figures above the table, summed from the rows rather than asked for separately. */
interface ReportTotals {
  readonly sprints: number;
  readonly tasks: number;
  readonly completed: number;
  readonly cancelled: number;
}

/**
 * Every sprint's outcome, for the administrator.
 *
 * Read-only, and admin-only on the server as well as here: the endpoint compares the caller against
 * the configured admin id and answers 403 otherwise. The guard on `/admin` is a convenience.
 *
 * Each row opens on its own to reveal what was actually in the sprint. Nothing is fetched until a
 * row is opened, and what has been fetched is kept for the rest of the visit — the same shape the
 * system-wide Sprints page uses.
 */
@Component({
  selector: 'app-sprint-report',
  templateUrl: './sprint-report.html',
  standalone: false,
  styleUrl: './sprint-report.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SprintReport {
  private readonly sprintReportService = inject(SprintReportService);
  private readonly sprintService = inject(SprintService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly failed = signal(false);

  protected readonly rows = signal<readonly SprintReportRow[]>([]);

  /** Which rows are open. Several may be at once — each is its own disclosure. */
  private readonly expanded = signal<ReadonlySet<number>>(new Set());

  /** Tasks per sprint, kept once loaded so collapsing and reopening costs no request. */
  private readonly tasksBySprint = signal<ReadonlyMap<number, TaskState>>(new Map());

  protected readonly skeletonRows = [0, 1, 2, 3, 4];

  protected readonly totals = computed<ReportTotals>(() => {
    const rows = this.rows();

    return {
      sprints: rows.length,
      tasks: rows.reduce((sum, row) => sum + row.totalTasks, 0),
      completed: rows.reduce((sum, row) => sum + row.completedCount, 0),
      cancelled: rows.reduce((sum, row) => sum + row.cancelledCount, 0)
    };
  });

  constructor() {
    this.sprintReportService.list()
      .pipe(takeUntilDestroyed())
      .subscribe({
        next: rows => {
          this.rows.set(rows);
          this.loading.set(false);
        },
        error: () => {
          // The message is a local constant; the server's text is never shown.
          this.failed.set(true);
          this.loading.set(false);
        }
      });
  }

  protected isExpanded(sprintId: number): boolean {
    return this.expanded().has(sprintId);
  }

  protected taskStateOf(sprintId: number): TaskState | undefined {
    return this.tasksBySprint().get(sprintId);
  }

  /**
   * Opens or closes one row. Closing keeps whatever was loaded, so opening it again is instant and
   * makes no second request.
   */
  protected toggle(sprintId: number): void {
    const open = new Set(this.expanded());

    if (open.delete(sprintId)) {
      this.expanded.set(open);

      return;
    }

    open.add(sprintId);
    this.expanded.set(open);

    // Already fetched, or already being fetched: nothing to do.
    if (this.tasksBySprint().has(sprintId)) {
      return;
    }

    this.setTaskState(sprintId, NO_TASKS);

    this.sprintService.tasks(sprintId)
      // Explicit DestroyRef: this runs from a click, outside the constructor's injection context.
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (tasks: readonly SprintTask[]) =>
          this.setTaskState(sprintId, SprintReport.group(tasks)),
        error: () => this.setTaskState(sprintId, { ...NO_TASKS, status: 'failed' })
      });
  }

  protected boardSprintsPath(boardId: number): string {
    return `/admin/boards/${boardId}/sprints`;
  }

  /**
   * Splits a sprint's tasks the same three ways the server counted them.
   *
   * Ongoing is everything that is neither completed nor cancelled — To Do and every custom column —
   * which is the server's definition exactly, so the group sizes always match the row's figures.
   */
  private static group(tasks: readonly SprintTask[]): TaskState {
    return {
      status: 'ready',
      completed: tasks.filter(task => task.systemKind === 'Completed'),
      cancelled: tasks.filter(task => task.systemKind === 'Cancelled'),
      ongoing: tasks.filter(
        task => task.systemKind !== 'Completed' && task.systemKind !== 'Cancelled')
    };
  }

  private setTaskState(sprintId: number, state: TaskState): void {
    const next = new Map(this.tasksBySprint());
    next.set(sprintId, state);
    this.tasksBySprint.set(next);
  }
}
