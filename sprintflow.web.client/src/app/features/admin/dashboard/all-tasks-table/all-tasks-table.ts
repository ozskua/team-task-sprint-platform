import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';

import { AdminDashboardTask } from '../../../../core/models/admin-dashboard-models';

export type TaskStatusFilter = 'All' | 'ToDo' | 'Open' | 'Completed' | 'Cancelled';

@Component({
  selector: 'app-all-tasks-table',
  templateUrl: './all-tasks-table.html',
  standalone: false,
  styleUrl: './all-tasks-table.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AllTasksTable {
  readonly tasks = input.required<readonly AdminDashboardTask[]>();
  readonly loading = input(false);
  readonly failed = input(false);

  protected readonly searchTerm = signal('');
  protected readonly statusFilter = signal<TaskStatusFilter>('All');

  protected readonly skeletonRows = [0, 1, 2, 3, 4];

  protected readonly statusOptions: readonly TaskStatusFilter[] =
    ['All', 'ToDo', 'Open', 'Completed', 'Cancelled'];

  protected readonly visibleTasks = computed(() => {
    const status = this.statusFilter();
    const term = AllTasksTable.normalize(this.searchTerm());

    return this.tasks()
      .filter(task => AllTasksTable.matchesStatus(task, status))
      .filter(task => term.length === 0 || AllTasksTable.matchesSearch(task, term));
  });

  protected readonly noMatches = computed(() =>
    this.tasks().length > 0 && this.visibleTasks().length === 0);

  protected onSearch(term: string): void {
    this.searchTerm.set(term);
  }

  protected onStatusChange(value: string): void {
    this.statusFilter.set(value as TaskStatusFilter);
  }

  protected statusLabel(status: TaskStatusFilter): string {
    return status === 'ToDo' ? 'To Do' : status;
  }

  protected chipClass(task: AdminDashboardTask): string {
    switch (task.systemKind) {
      case 'ToDo': return 'column-chip--todo';
      case 'Completed': return 'column-chip--completed';
      case 'Cancelled': return 'column-chip--cancelled';
      default: return 'column-chip--open';
    }
  }

  private static matchesStatus(task: AdminDashboardTask, status: TaskStatusFilter): boolean {
    if (status === 'All') {
      return true;
    }

    if (status === 'Open') {
      return task.systemKind === null;
    }

    return task.systemKind === status;
  }

  private static matchesSearch(task: AdminDashboardTask, term: string): boolean {
    return AllTasksTable.normalize(task.title).includes(term)
      || AllTasksTable.normalize(task.areaName).includes(term)
      || AllTasksTable.normalize(task.boardName).includes(term)
      || AllTasksTable.normalize(task.assignee?.fullName ?? '').includes(term)
      || AllTasksTable.normalize(task.createdByFullName).includes(term);
  }

  private static normalize(value: string): string {
    return value.trim().toLocaleLowerCase('tr');
  }
}
