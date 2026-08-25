import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { BoardDetail } from '../../../core/models/board-detail-models';
import { initialsOf } from '../../../shared/initials';

/**
 * What the board is, who made it, and who is on its team.
 *
 * The creator and the team members are shown as two separate things on purpose. Creating a board
 * records ownership; it does not put anyone on the team. Membership comes from being assigned a
 * task on the board, so a board can perfectly well have a creator and no members at all.
 */
@Component({
  selector: 'app-board-header',
  templateUrl: './board-header.html',
  standalone: false,
  styleUrl: './board-header.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BoardHeader {
  readonly board = input.required<BoardDetail>();

  /**
   * One flag per action rather than a single "show the actions" switch, so a view can drop the one
   * thing that does not apply to it without losing the rest. Only the backlog uses them, and only
   * to hide Add column: it has no columns on screen to add one next to, while creating a task,
   * renaming the board and deleting it all mean exactly what they mean on the kanban.
   */
  readonly showAddColumn = input(true);
  readonly showNewTask = input(true);
  readonly showBoardMenu = input(true);

  /**
   * Whether a task can be created from here.
   *
   * False on a kanban with no sprint running: the task would belong to no sprint and so would not
   * appear in the view it was made from. The backlog is where an unsprinted task is created.
   */
  readonly canCreateTask = input(true);

  readonly newTask = output<void>();
  readonly newColumn = output<void>();
  readonly renameBoard = output<void>();
  readonly deleteBoard = output<void>();

  protected initials(fullName: string): string {
    return initialsOf(fullName);
  }
}
