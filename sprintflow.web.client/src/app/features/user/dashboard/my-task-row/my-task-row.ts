import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { DashboardTask } from '../../../../core/models/dashboard-models';

@Component({
  selector: 'app-my-task-row',
  templateUrl: './my-task-row.html',
  standalone: false,
  styleUrl: './my-task-row.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MyTaskRow {
  readonly task = input.required<DashboardTask>();

  protected readonly chipClass = computed(() =>
    this.task().systemKind === 'ToDo' ? 'column-chip--todo' : 'column-chip--open');
}
