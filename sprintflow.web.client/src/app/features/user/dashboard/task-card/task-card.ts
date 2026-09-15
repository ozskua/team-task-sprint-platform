import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { UserTask } from '../user-dashboard-models';

@Component({
  selector: 'app-task-card',
  templateUrl: './task-card.html',
  standalone: false,
  styleUrl: './task-card.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TaskCard {
  readonly task = input.required<UserTask>();

  readonly overdue = input(false);

  protected readonly dueLabel = computed(() => {
    const due = this.task().dueDate;
    if (due === null) {
      return 'No due date';
    }

    return new Date(due).toLocaleDateString('en-GB', {
      day: 'numeric',
      month: 'short',
      year: 'numeric'
    });
  });
}
