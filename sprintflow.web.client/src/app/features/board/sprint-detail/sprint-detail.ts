import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';

import { SprintDetail as SprintDetailModel, SprintTask } from '../../../core/models/sprint-models';
import { SprintService } from '../../../core/services/sprint.service';
import { TaskTableRow } from '../../../shared/task-table/task-table';

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

  protected readonly taskRows = computed<readonly TaskTableRow[]>(() => this.tasks());

  constructor() {
    this.route.paramMap
      .pipe(takeUntilDestroyed())
      .subscribe(params => this.load(Number(params.get('sprintId'))));
  }

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
