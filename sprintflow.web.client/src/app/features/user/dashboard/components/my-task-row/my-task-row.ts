import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { DashboardTask } from '../../../../../core/models/dashboard-models';

/**
 * One task assigned to the signed-in user, as a row.
 *
 * The whole row links to the board the task sits on. There is no task URL — a task is opened in a
 * dialog from inside its board — so the board is the only honest destination.
 */
@Component({
  selector: 'app-my-task-row',
  templateUrl: './my-task-row.html',
  standalone: false,
  styleUrl: './my-task-row.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MyTaskRow {
  readonly task = input.required<DashboardTask>();

  /**
   * Custom columns all share one tint. A board may define any number of them, so a colour per name
   * would be decoration rather than information.
   */
  protected readonly chipClass = computed(() =>
    this.task().systemKind === 'ToDo' ? 'column-chip--todo' : 'column-chip--open');
}
