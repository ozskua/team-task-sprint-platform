import { ChangeDetectionStrategy, Component, TemplateRef, input, output } from '@angular/core';

import { initialsOf } from '../initials';

/**
 * One row of the task table, in the only shape it needs.
 *
 * Deliberately not `BoardTask`: the backlog holds tasks plus the columns they sit in, while the
 * sprint pages receive a flattened row from the server. Normalising to this lets one table serve
 * both without either caller bending to the other's model.
 */
export interface TaskTableRow {
  readonly taskId: number;
  readonly title: string;

  /** The column the task is in — its status, in the board's own words. */
  readonly columnName: string;

  /** `null` for a custom column. Decides whether the status gets an outcome dot. */
  readonly systemKind: 'ToDo' | 'Completed' | 'Cancelled' | null;

  readonly assigneeFullName: string | null;
  readonly createdByFullName: string;

  /** ISO date-time without an offset, rendered as-is. */
  readonly createdDate: string;
}

/**
 * The task table, as the Backlog has always drawn it: task, status, assignee, creator, created.
 *
 * Extracted from the Backlog so the sprint pages can show the same rows rather than a second table
 * that drifts away from it. The Backlog keeps everything it had by passing its two extra columns —
 * the sprint picker and the status picker — in as templates; the sprint pages pass none and get a
 * read-only table.
 */
@Component({
  selector: 'app-task-table',
  templateUrl: './task-table.html',
  standalone: false,
  styleUrl: './task-table.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TaskTable {
  readonly rows = input.required<readonly TaskTableRow[]>();

  /** Screen-reader caption. Says which board or sprint these rows belong to. */
  readonly caption = input.required<string>();

  /** Rows whose pending change should read as busy. */
  readonly busyTaskIds = input<ReadonlySet<number>>(new Set<number>());

  /**
   * Extra header cells, appended after `Created`. Supplied together with {@link extraCells}, or
   * neither — a header with no matching body cell would knock the columns out of line.
   */
  readonly extraHeaders = input<TemplateRef<unknown> | null>(null);

  /** Extra body cells for each row, receiving that row as the implicit context. */
  readonly extraCells = input<TemplateRef<{ $implicit: TaskTableRow }> | null>(null);

  /**
   * Whether the title opens something. The Backlog opens the task dialog; the sprint pages have
   * nowhere to open it, so there the title is plain text rather than a button that looks clickable
   * and is not.
   */
  readonly titleOpensTask = input(false);

  readonly openTask = output<number>();

  protected initials(fullName: string): string {
    return initialsOf(fullName);
  }

  /** Completed and Cancelled stay two separate statuses here, whatever the kanban draws. */
  protected outcomeOf(row: TaskTableRow): 'Completed' | 'Cancelled' | null {
    return row.systemKind === 'Completed' || row.systemKind === 'Cancelled'
      ? row.systemKind
      : null;
  }

  protected isBusy(taskId: number): boolean {
    return this.busyTaskIds().has(taskId);
  }
}
