import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { BoardTask } from '../../../core/models/board-detail-models';
import { initialsOf } from '../../../shared/initials';
import { taskPriorityLabel } from '../task-priority';

/** Which outcome a card is in, or nothing at all for the columns that are still in progress. */
export type TaskOutcome = 'Completed' | 'Cancelled';

/** One card on the board. */
@Component({
  selector: 'app-task-card',
  templateUrl: './task-card.html',
  standalone: false,
  styleUrl: './task-card.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TaskCard {
  readonly task = input.required<BoardTask>();

  /** True while this card's own move is still in flight. */
  readonly moving = input(false);

  /**
   * Set only by the outcome lane. To Do and the custom columns leave it null and stay neutral —
   * a board where every card is coloured says nothing.
   */
  readonly outcome = input<TaskOutcome | null>(null);

  readonly open = output<BoardTask>();
  readonly edit = output<BoardTask>();
  readonly remove = output<BoardTask>();

  /** Names the avatar for a screen reader and for anyone hovering it. */
  protected readonly assigneeLabel = computed(() => {
    const assignee = this.task().assignee;

    return assignee === null ? 'Unassigned' : `Assigned to ${assignee.fullName}`;
  });

  protected readonly outcomeIcon = computed(() =>
    this.outcome() === 'Completed' ? 'check' : 'close');

  protected readonly priorityLabel = computed(() => taskPriorityLabel(this.task().priority));

  /**
   * The card itself opens the task. Anything the user could have meant instead — the menu button,
   * the title button — is a button, so one check covers all of them and keeps covering them if
   * another control is added later.
   */
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
