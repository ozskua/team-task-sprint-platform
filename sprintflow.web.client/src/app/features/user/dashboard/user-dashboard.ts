import { ChangeDetectionStrategy, Component, computed, signal } from '@angular/core';

import {
  EMPTY_TASK_COUNTS,
  TaskCounts,
  UserTask,
  UserTeam
} from './user-dashboard-models';

/** Both lists show at most this many cards. */
const MAX_CARDS = 5;

@Component({
  selector: 'app-user-dashboard',
  templateUrl: './user-dashboard.html',
  standalone: false,
  styleUrl: './user-dashboard.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UserDashboard {
  /**
   * There is no API yet, so the page renders its empty state — which is also the
   * truthful state of a user with no teams. When the endpoint exists it sets
   * these same signals and nothing else changes.
   */
  protected readonly loading = signal(false);
  protected readonly counts = signal<TaskCounts>(EMPTY_TASK_COUNTS);
  protected readonly teams = signal<readonly UserTeam[]>([]);

  /** Every task the user is allowed to see, i.e. from their own teams only. */
  protected readonly tasks = signal<readonly UserTask[]>([]);

  protected readonly totalTasks = computed(() => {
    const { todo, inProgress, completed } = this.counts();
    return todo + inProgress + completed;
  });

  protected readonly openTaskCount = computed(() => {
    const { todo, inProgress } = this.counts();
    return todo + inProgress;
  });

  /** Completed / (Todo + In Progress + Completed). Zero tasks reads as 0%. */
  protected readonly progressPercent = computed(() => {
    const total = this.totalTasks();
    return total === 0 ? 0 : Math.round((this.counts().completed / total) * 100);
  });

  /** Open means not Completed. Completed tasks never appear in either list. */
  private readonly openTasks = computed(() =>
    this.tasks().filter(task => task.status !== 'Completed')
  );

  /** Overdue: the due date has passed and the task is not Completed. */
  protected readonly overdueTasks = computed(() => {
    const today = this.startOfToday();

    return this.openTasks()
      .filter(task => task.dueDate !== null && new Date(task.dueDate) < today)
      .sort(this.byDueDate)
      .slice(0, MAX_CARDS);
  });

  /** The nearest open tasks that are not already late, soonest first. */
  protected readonly upcomingTasks = computed(() => {
    const today = this.startOfToday();

    return this.openTasks()
      .filter(task => task.dueDate === null || new Date(task.dueDate) >= today)
      .sort(this.byDueDate)
      .slice(0, MAX_CARDS);
  });

  protected readonly hasOverdue = computed(() => this.overdueTasks().length > 0);

  protected readonly contextLine = computed(() => {
    const teamCount = this.teams().length;

    if (teamCount === 0) {
      return 'You are not a member of any team yet.';
    }

    const tasks = this.openTaskCount();

    return `You have ${tasks} open ${tasks === 1 ? 'task' : 'tasks'} `
      + `across ${teamCount} ${teamCount === 1 ? 'team' : 'teams'}.`;
  });

  /** First letter of the team name, used as a simple monogram on team cards. */
  protected initialOf(team: UserTeam): string {
    return team.teamName.charAt(0).toUpperCase();
  }

  /** Midnight today, so a task due today does not count as late. */
  private startOfToday(): Date {
    const now = new Date();
    return new Date(now.getFullYear(), now.getMonth(), now.getDate());
  }

  /** Soonest deadline first; tasks without a due date go last. */
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
