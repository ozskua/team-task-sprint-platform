import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import {
  DashboardTask,
  DashboardTaskCounts,
  DashboardTeam
} from '../../../core/models/dashboard-models';
import { DashboardService } from '../../../core/services/dashboard.service';
import {
  EMPTY_TASK_COUNTS,
  TaskCounts,
  UserTask,
  UserTeam
} from './user-dashboard-models';

const MAX_CARDS = 5;

const EMPTY_COUNTS: DashboardTaskCounts = { toDo: 0, open: 0, completed: 0, cancelled: 0 };

@Component({
  selector: 'app-user-dashboard',
  templateUrl: './user-dashboard.html',
  standalone: false,
  styleUrl: './user-dashboard.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UserDashboard {
  private readonly dashboardService = inject(DashboardService);

  protected readonly loading = signal(true);
  protected readonly failed = signal(false);

  protected readonly myCounts = signal<DashboardTaskCounts>(EMPTY_COUNTS);
  protected readonly myTasks = signal<readonly DashboardTask[]>([]);
  protected readonly myTeams = signal<readonly DashboardTeam[]>([]);

  protected readonly skeletonRows = [0, 1, 2];

  constructor() {
    this.dashboardService.get()
      .pipe(takeUntilDestroyed())
      .subscribe({
        next: dashboard => {
          this.myCounts.set(dashboard.counts);
          this.myTasks.set(dashboard.myTasks);
          this.myTeams.set(dashboard.myTeams);
          this.loading.set(false);
        },
        error: () => {
          this.failed.set(true);
          this.loading.set(false);
        }
      });
  }

protected readonly openTaskCount = computed(() => {
    const { toDo, open } = this.myCounts();

    return toDo + open;
  });

  protected readonly progressPercent = computed(() => {
    const { toDo, open, completed } = this.myCounts();
    const total = toDo + open + completed;

    return total === 0 ? 0 : Math.round((completed / total) * 100);
  });

  protected readonly contextLine = computed(() => {
    const teams = this.myTeams().length;

    if (teams === 0) {
      return 'Nothing is assigned to you yet.';
    }

    const teamLabel = `${teams} ${teams === 1 ? 'team' : 'teams'}`;
    const open = this.openTaskCount();

    if (open === 0) {
      return `You have nothing assigned right now, across ${teamLabel}.`;
    }

    return `You have ${open} open ${open === 1 ? 'task' : 'tasks'} across ${teamLabel}.`;
  });

protected readonly counts = signal<TaskCounts>(EMPTY_TASK_COUNTS);
  protected readonly teams = signal<readonly UserTeam[]>([]);
  protected readonly tasks = signal<readonly UserTask[]>([]);

  private readonly openTasks = computed(() =>
    this.tasks().filter(task => task.status !== 'Completed')
  );

  protected readonly overdueTasks = computed(() => {
    const today = this.startOfToday();

    return this.openTasks()
      .filter(task => task.dueDate !== null && new Date(task.dueDate) < today)
      .sort(this.byDueDate)
      .slice(0, MAX_CARDS);
  });

  protected readonly upcomingTasks = computed(() => {
    const today = this.startOfToday();

    return this.openTasks()
      .filter(task => task.dueDate === null || new Date(task.dueDate) >= today)
      .sort(this.byDueDate)
      .slice(0, MAX_CARDS);
  });

  protected readonly hasOverdue = computed(() => this.overdueTasks().length > 0);

  private startOfToday(): Date {
    const now = new Date();

    return new Date(now.getFullYear(), now.getMonth(), now.getDate());
  }

  private readonly byDueDate = (a: UserTask, b: UserTask): number => {
    if (a.dueDate === null) {
      return b.dueDate === null ? 0 : 1;
    }
    if (b.dueDate === null) {
      return -1;
    }

    return a.dueDate.localeCompare(b.dueDate);
  };
}
