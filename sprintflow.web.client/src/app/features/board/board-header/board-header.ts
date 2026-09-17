import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { BoardDetail } from '../../../core/models/board-detail-models';

@Component({
  selector: 'app-board-header',
  templateUrl: './board-header.html',
  standalone: false,
  styleUrl: './board-header.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BoardHeader {
  readonly board = input.required<BoardDetail>();

  readonly showAddColumn = input(true);
  readonly showNewTask = input(true);
  readonly showBoardMenu = input(true);

  readonly canCreateTask = input(true);

  readonly newTask = output<void>();
  readonly newColumn = output<void>();
  readonly renameBoard = output<void>();
  readonly deleteBoard = output<void>();
}
