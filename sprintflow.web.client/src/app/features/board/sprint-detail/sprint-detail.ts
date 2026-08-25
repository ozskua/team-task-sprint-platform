import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';

import { SprintDetail as SprintDetailModel, SprintTask } from '../../../core/models/sprint-models';
import { SprintService } from '../../../core/services/sprint.service';
import { TaskTableRow } from '../../../shared/task-table/task-table';

/**
 * One sprint's own page: what it is, and what is in it.
 *
 * The tasks are listed here rather than behind a link to the board, because "which work is in this
 * sprint" is the question the page exists to answer — sending the reader to the kanban to find out
 * would make them navigate for something the page already knows.
 *
 * Read-only. Editing and deleting a sprint are not part of this phase, and the task rows are drawn
 * by the same table the Backlog uses, without its selectors: moving work between columns or sprints
 * belongs on the Backlog, which has the whole board in view.
 */
@Component({
  selector: 'app-sprint-detail',
  templateUrl: './sprint-detail.html',
  standalone: false,
  styleUrl: './sprint-detail.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SprintDetail {
  private readonly sprintService = inject(SprintService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly sprint = signal<SprintDetailModel | null>(null);

  protected readonly loading = signal(true);
  protected readonly failed = signal(false);

  private readonly tasks = signal<readonly SprintTask[]>([]);
  protected readonly tasksLoading = signal(true);
  protected readonly tasksFailed = signal(false);

  /** The server already sends the shape the table wants; this only renames nothing. */
  protected readonly taskRows = computed<readonly TaskTableRow[]>(() => this.tasks());

  constructor() {
    this.route.paramMap
      .pipe(takeUntilDestroyed())
      .subscribe(params => this.load(Number(params.get('sprintId'))));
  }

  /** '' in the member shell and '/admin' in the other. */
  private shellPrefix(): string {
    return this.router.url.split('/boards/')[0];
  }

  protected sprintsPath(): string {
    return `${this.shellPrefix()}/boards/${this.sprint()?.boardId}/sprints`;
  }

  private load(sprintId: number): void {
    if (!Number.isInteger(sprintId) || sprintId <= 0) {
      this.loading.set(false);
      this.failed.set(true);

      return;
    }

    this.loading.set(true);
    this.failed.set(false);

    this.sprintService.get(sprintId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: sprint => {
          this.sprint.set(sprint);
          this.loading.set(false);
        },
        error: () => {
          this.failed.set(true);
          this.loading.set(false);
        }
      });

    // A second call rather than part of the sprint payload: the fact card renders as soon as it can
    // and the list fills in after, instead of the whole page waiting on the longer query.
    this.tasksLoading.set(true);
    this.tasksFailed.set(false);

    this.sprintService.tasks(sprintId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: tasks => {
          this.tasks.set(tasks);
          this.tasksLoading.set(false);
        },
        error: () => {
          this.tasksFailed.set(true);
          this.tasksLoading.set(false);
        }
      });
  }
}
