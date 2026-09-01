import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';

import { AdminDashboardTask } from '../../../../core/models/admin-dashboard-models';
import { initialsOf } from '../../../../shared/initials';

/** The buckets a reader can narrow to. `All` is the default and hides nothing. */
export type TaskStatusFilter = 'All' | 'ToDo' | 'Open' | 'Completed' | 'Cancelled';

/**
 * Every task in the system, as a table.
 *
 * Search and the status filter live here rather than on the page: they are properties of this table,
 * and the whole task set already arrived in one payload, so filtering in the browser costs nothing
 * and avoids a request per keystroke.
 */
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

  /** Rows drawn while loading, so the table keeps its shape. */
  protected readonly skeletonRows = [0, 1, 2, 3, 4];

  protected readonly statusOptions: readonly TaskStatusFilter[] =
    ['All', 'ToDo', 'Open', 'Completed', 'Cancelled'];

  /**
   * The filter narrows first, then the search matches within what is left — so the two compose
   * rather than one replacing the other.
   */
  protected readonly visibleTasks = computed(() => {
    const status = this.statusFilter();
    const term = AllTasksTable.normalize(this.searchTerm());

    return this.tasks()
      .filter(task => AllTasksTable.matchesStatus(task, status))
      .filter(task => term.length === 0 || AllTasksTable.matchesSearch(task, term));
  });

  /** Distinguishes "nothing here" from "nothing matched what you typed". */
  protected readonly noMatches = computed(() =>
    this.tasks().length > 0 && this.visibleTasks().length === 0);

  protected onSearch(term: string): void {
    this.searchTerm.set(term);
  }

  protected onStatusChange(value: string): void {
    this.statusFilter.set(value as TaskStatusFilter);
  }

  /** "Open" reads better than "Open" alone in a dropdown of system column names. */
  protected statusLabel(status: TaskStatusFilter): string {
    return status === 'ToDo' ? 'To Do' : status;
  }

  /**
   * Custom columns share one tint. A board may define any number of them, so a colour per name
   * would be decoration rather than information.
   */
  protected chipClass(task: AdminDashboardTask): string {
    switch (task.systemKind) {
      case 'ToDo': return 'column-chip--todo';
      case 'Completed': return 'column-chip--completed';
      case 'Cancelled': return 'column-chip--cancelled';
      default: return 'column-chip--open';
    }
  }

  protected initials(fullName: string): string {
    return initialsOf(fullName);
  }

  private static matchesStatus(task: AdminDashboardTask, status: TaskStatusFilter): boolean {
    if (status === 'All') {
      return true;
    }

    // "Open" is every custom column, which is exactly the columns with no system kind — the same
    // definition the summary above the table uses.
    if (status === 'Open') {
      return task.systemKind === null;
    }

    return task.systemKind === status;
  }

  /** Every field the row shows is searchable, so anything visible is findable. */
  private static matchesSearch(task: AdminDashboardTask, term: string): boolean {
    return AllTasksTable.normalize(task.title).includes(term)
      || AllTasksTable.normalize(task.areaName).includes(term)
      || AllTasksTable.normalize(task.boardName).includes(term)
      || AllTasksTable.normalize(task.assignee?.fullName ?? '').includes(term)
      || AllTasksTable.normalize(task.createdByFullName).includes(term);
  }

  /**
   * Turkish casing: 'İ' lowercases to 'i' here, where the invariant rule would produce 'i' plus a
   * combining dot and stop the search matching.
   */
  private static normalize(value: string): string {
    return value.trim().toLocaleLowerCase('tr');
  }
}
