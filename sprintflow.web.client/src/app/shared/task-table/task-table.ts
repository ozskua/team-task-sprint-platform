import { ChangeDetectionStrategy, Component, TemplateRef, input, output } from '@angular/core';

export interface TaskTableRow {
  readonly taskId: number;
  readonly title: string;

  readonly columnName: string;

  readonly systemKind: 'ToDo' | 'Completed' | 'Cancelled' | null;

  readonly assigneeUserId: number | null;
  readonly assigneeFullName: string | null;
  readonly createdByFullName: string;

  readonly createdDate: string;
}

@Component({
  selector: 'app-task-table',
  templateUrl: './task-table.html',
  standalone: false,
  styleUrl: './task-table.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TaskTable {
  readonly rows = input.required<readonly TaskTableRow[]>();

  readonly caption = input.required<string>();

  readonly busyTaskIds = input<ReadonlySet<number>>(new Set<number>());

  readonly extraHeaders = input<TemplateRef<unknown> | null>(null);

  readonly extraCells = input<TemplateRef<{ $implicit: TaskTableRow }> | null>(null);

  readonly titleOpensTask = input(false);

  readonly openTask = output<number>();

  protected outcomeOf(row: TaskTableRow): 'Completed' | 'Cancelled' | null {
    return row.systemKind === 'Completed' || row.systemKind === 'Cancelled'
      ? row.systemKind
      : null;
  }

  protected isBusy(taskId: number): boolean {
    return this.busyTaskIds().has(taskId);
  }
}
