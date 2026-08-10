import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { UserTask } from '../../user-dashboard-models';

/**
 * Displays one task: title, team, due date and status.
 *
 * Presentation only. It does not navigate anywhere, because no task detail or
 * board route exists yet and inventing one would be a fake destination. When a
 * real target exists, this gains one optional input and an <a>/<div> switch.
 */
@Component({
  selector: 'app-task-card',
  templateUrl: './task-card.html',
  standalone: false,
  styleUrl: './task-card.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TaskCard {
  readonly task = input.required<UserTask>();

  /** Set by the Needs Attention section so the due date reads as late. */
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
