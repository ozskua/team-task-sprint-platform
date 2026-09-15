import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { BoardTask } from '../../../core/models/board-detail-models';
import { initialsOf } from '../../../shared/initials';
import { taskPriorityLabel } from '../task-priority';

export type TaskOutcome = 'Completed' | 'Cancelled';

@Component({
  selector: 'app-task-card',
  templateUrl: './task-card.html',
  standalone: false,
  styleUrl: './task-card.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TaskCard {
  readonly task = input.required<BoardTask>();

  readonly moving = input(false);

  readonly outcome = input<TaskOutcome | null>(null);

  readonly open = output<BoardTask>();
  readonly edit = output<BoardTask>();
  readonly remove = output<BoardTask>();

  protected readonly assigneeLabel = computed(() => {
    const assignee = this.task().assignee;

    return assignee === null ? 'Unassigned' : `Assigned to ${assignee.fullName}`;
  });

  protected readonly outcomeIcon = computed(() =>
    this.outcome() === 'Completed' ? 'check' : 'close');

  protected readonly priorityLabel = computed(() => taskPriorityLabel(this.task().priority));

  protected onCardClick(event: MouseEvent): void {
    if ((event.target as HTMLElement).closest('button, a') !== null) {
      return;
    }

    this.open.emit(this.task());
  }

  protected initials(fullName: string): string {
    return initialsOf(fullName);
  }
}
