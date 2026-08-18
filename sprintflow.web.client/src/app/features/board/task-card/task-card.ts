import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { BoardTask } from '../../../core/models/board-detail-models';
import { initialsOf } from '../../../shared/initials';

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

  readonly open = output<BoardTask>();
  readonly edit = output<BoardTask>();
  readonly remove = output<BoardTask>();

  /** Names the avatar for a screen reader and for anyone hovering it. */
  protected readonly assigneeLabel = computed(() => {
    const assignee = this.task().assignee;

    return assignee === null ? 'Unassigned' : `Assigned to ${assignee.fullName}`;
  });

  protected initials(fullName: string): string {
    return initialsOf(fullName);
  }
}
