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

  readonly newTask = output<void>();
  readonly newColumn = output<void>();
  readonly renameBoard = output<void>();
  readonly deleteBoard = output<void>();

  protected initials(fullName: string): string {
    return initialsOf(fullName);
  }
}
