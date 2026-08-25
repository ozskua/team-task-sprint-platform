import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  signal
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';

import { SprintListItem, SprintTask } from '../../../core/models/sprint-models';
import { SprintService } from '../../../core/services/sprint.service';
import { TaskTableRow } from '../../../shared/task-table/task-table';

/** One board's sprints, kept together. The heading names the board and the area it sits in. */
interface BoardGroup {
  readonly boardId: number;
  readonly boardName: string;
  readonly areaName: string;
  readonly sprints: readonly SprintListItem[];
}

/** What is known about one sprint's tasks, once someone has opened it. */
interface TaskState {
  readonly status: 'loading' | 'ready' | 'failed';
  readonly rows: readonly TaskTableRow[];
}

/**
 * Every sprint in the system, grouped by the board that owns it.
 *
 * Read-only. A sprint belongs to a board, and creating or editing one happens on that board's
 * Sprint tab — this page is for finding out what is scheduled where, not for managing it.
 *
 * Grouped rather than listed flat because sprints only avoid overlapping *within* a board: two
 * boards may legitimately run sprints over the same days, and a single date-ordered list would show
 * that as though the rule had been broken.
 *
 * Each row opens on its own to reveal its tasks. Nothing is fetched until a row is opened, and what
 * has been fetched is kept for the rest of the visit — reopening a row costs nothing.
 */
@Component({
  selector: 'app-all-sprints',
  templateUrl: './all-sprints.html',
  standalone: false,
  styleUrl: './all-sprints.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AllSprints {
  private readonly sprintService = inject(SprintService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly failed = signal(false);

  private readonly sprints = signal<readonly SprintListItem[]>([]);

  /** Which rows are open. Several may be at once — each is its own disclosure. */
  private readonly expanded = signal<ReadonlySet<number>>(new Set());

  /** Tasks per sprint, kept once loaded so collapsing and reopening costs no request. */
  private readonly tasksBySprint = signal<ReadonlyMap<number, TaskState>>(new Map());

  protected readonly skeletonRows = [0, 1, 2];

  /**
   * The server already orders by area, then board, then start day, so the groups are formed by
   * walking the list once and nothing here re-sorts anything.
   */
  protected readonly groups = computed<readonly BoardGroup[]>(() => {
    // Built with a mutable sprint list and returned as BoardGroup, whose own is readonly — so the
    // accumulation stays honest instead of casting the readonly away.
    const groups: { boardId: number; boardName: string; areaName: string; sprints: SprintListItem[] }[] = [];

    for (const sprint of this.sprints()) {
      const current = groups.at(-1);

      if (current?.boardId === sprint.boardId) {
        current.sprints.push(sprint);
      } else {
        groups.push({
          boardId: sprint.boardId,
          boardName: sprint.boardName,
          areaName: sprint.areaName,
          sprints: [sprint]
        });
      }
    }

    return groups;
  });

  constructor() {
    this.sprintService.listAll()
      .pipe(takeUntilDestroyed())
      .subscribe({
        next: sprints => {
          this.sprints.set(sprints);
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

    this.setTaskState(sprintId, { status: 'loading', rows: [] });

    this.sprintService.tasks(sprintId)
      // Explicit DestroyRef: this runs from a click, outside the injection context the constructor
      // has.
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

  /**
   * '' in the member shell and '/admin' in the other, so one page links correctly from both.
   *
   * Read from this page's own URL rather than the board's, because unlike the board views this one
   * is not mounted under `/boards/`.
   */
  private shellPrefix(): string {
    return this.router.url.startsWith('/admin') ? '/admin' : '';
  }

  /** Opening a sprint's board means working in it: the kanban scoped to that sprint. */
  protected boardPath(boardId: number): string {
    return `${this.shellPrefix()}/boards/${boardId}`;
  }

  /** The board's own Sprint tab — where sprints are actually managed. */
  protected sprintsPath(boardId: number): string {
    return `${this.boardPath(boardId)}/sprints`;
  }
}
