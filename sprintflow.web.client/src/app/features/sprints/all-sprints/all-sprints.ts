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

interface BoardGroup {
  readonly boardId: number;
  readonly boardName: string;
  readonly areaName: string;
  readonly sprints: readonly SprintListItem[];
}

interface TaskState {
  readonly status: 'loading' | 'ready' | 'failed';
  readonly rows: readonly TaskTableRow[];
}

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

  private readonly expanded = signal<ReadonlySet<number>>(new Set());

  private readonly tasksBySprint = signal<ReadonlyMap<number, TaskState>>(new Map());

  protected readonly skeletonRows = [0, 1, 2];

  protected readonly groups = computed<readonly BoardGroup[]>(() => {
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

  protected toggle(sprintId: number): void {
    const open = new Set(this.expanded());

    if (open.delete(sprintId)) {
      this.expanded.set(open);

      return;
    }

    open.add(sprintId);
    this.expanded.set(open);

    if (this.tasksBySprint().has(sprintId)) {
      return;
    }

    this.setTaskState(sprintId, { status: 'loading', rows: [] });

    this.sprintService.tasks(sprintId)
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

  private shellPrefix(): string {
    return this.router.url.startsWith('/admin') ? '/admin' : '';
  }

  protected boardPath(boardId: number): string {
    return `${this.shellPrefix()}/boards/${boardId}`;
  }

  protected sprintsPath(boardId: number): string {
    return `${this.boardPath(boardId)}/sprints`;
  }
}
