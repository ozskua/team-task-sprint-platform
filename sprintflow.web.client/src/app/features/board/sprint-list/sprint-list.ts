import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRoute, Router } from '@angular/router';

import { Sprint, SprintTask } from '../../../core/models/sprint-models';
import { SprintService } from '../../../core/services/sprint.service';
import { TaskTableRow } from '../../../shared/task-table/task-table';
import { SprintFormDialog, SprintFormResult } from '../dialogs/sprint-form-dialog/sprint-form-dialog';

/** What is known about one sprint's tasks, once someone has opened it. */
interface TaskState {
  readonly status: 'loading' | 'ready' | 'failed';
  readonly rows: readonly TaskTableRow[];
}

/**
 * One board's sprints: what they are called, when they run, and how they stand.
 *
 * Sprints on a board form a single timeline, so this is a sequence rather than a set — it is
 * ordered by date and never by name or by when it was created.
 *
 * Each row opens on its own to reveal that sprint's tasks, the same way the global overview does.
 * Nothing is fetched until a row is opened, and what has been fetched is kept for the rest of the
 * visit. Working *in* a sprint is a different intent, so it keeps its own control: the scoped kanban.
 */
@Component({
  selector: 'app-sprint-list',
  templateUrl: './sprint-list.html',
  standalone: false,
  styleUrl: './sprint-list.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SprintList {
  private readonly sprintService = inject(SprintService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly boardId = signal(0);
  protected readonly sprints = signal<readonly Sprint[]>([]);

  protected readonly loading = signal(true);
  protected readonly failed = signal(false);

  /** A local, generic message. The server's own wording is never shown. */
  protected readonly actionError = signal<string | null>(null);

  /** Which rows are open. Several may be at once — each is its own disclosure. */
  private readonly expanded = signal<ReadonlySet<number>>(new Set());

  /** Tasks per sprint, kept once loaded so collapsing and reopening costs no request. */
  private readonly tasksBySprint = signal<ReadonlyMap<number, TaskState>>(new Map());

  protected readonly skeletonRows = [0, 1, 2];

  constructor() {
    this.route.paramMap
      .pipe(takeUntilDestroyed())
      .subscribe(params => this.load(Number(params.get('boardId'))));
  }

  protected dismissError(): void {
    this.actionError.set(null);
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

    this.setTaskState(sprintId, { status: 'loading', rows: [] });

    this.sprintService.tasks(sprintId)
      // Explicit DestroyRef: this runs from a click, outside the constructor's injection context.
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (tasks: readonly SprintTask[]) =>
          this.setTaskState(sprintId, { status: 'ready', rows: tasks }),
        error: () => this.setTaskState(sprintId, { status: 'failed', rows: [] })
      });
  }

  private setTaskState(sprintId: number, state: TaskState): void {
    const next = new Map(this.tasksBySprint());
    next.set(sprintId, state);
    this.tasksBySprint.set(next);
  }

  /** '' in the member shell and '/admin' in the other, so one component serves both. */
  protected boardPath(): string {
    return this.router.url.split('/boards/')[0] + `/boards/${this.boardId()}`;
  }

  protected create(): void {
    this.dialog
      .open<SprintFormDialog, undefined, SprintFormResult>(SprintFormDialog, { autoFocus: 'dialog' })
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(result => {
        if (result !== undefined) {
          this.submit(result);
        }
      });
  }

  private submit(result: SprintFormResult): void {
    this.actionError.set(null);

    this.sprintService.create(this.boardId(), result)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        // Re-read rather than appending: the list is ordered by date, and a new sprint rarely
        // belongs at the end of it.
        next: () => this.load(this.boardId()),
        error: () => this.actionError.set(
          'That sprint could not be created. Its dates may overlap another sprint on this board.')
      });
  }

  private load(boardId: number): void {
    if (!Number.isInteger(boardId) || boardId <= 0) {
      this.loading.set(false);
      this.failed.set(true);

      return;
    }

    this.boardId.set(boardId);
    this.loading.set(true);
    this.failed.set(false);

    // Dropped on every load: a re-read after creating a sprint should not leave rows open over
    // tasks fetched before it, and another board's sprint ids mean nothing here.
    this.expanded.set(new Set());
    this.tasksBySprint.set(new Map());

    this.sprintService.list(boardId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: sprints => {
          this.sprints.set(sprints);
          this.loading.set(false);
        },
        error: () => {
          this.failed.set(true);
          this.loading.set(false);
        }
      });
  }
}
