import { CdkDragDrop } from '@angular/cdk/drag-drop';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { BoardColumn, BoardTask } from '../../../core/models/board-detail-models';

@Component({
  selector: 'app-outcome-lane',
  templateUrl: './outcome-lane.html',
  standalone: false,
  styleUrl: './outcome-lane.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OutcomeLane {
  readonly completed = input.required<BoardColumn>();
  readonly cancelled = input.required<BoardColumn>();

  readonly connectedTo = input.required<string[]>();

  readonly movingTaskIds = input.required<ReadonlySet<number>>();

  readonly openTask = output<BoardTask>();
  readonly editTask = output<BoardTask>();
  readonly deleteTask = output<BoardTask>();
  readonly taskDropped = output<CdkDragDrop<BoardColumn>>();

  protected dropListId(column: BoardColumn): string {
    return `tasks-${column.workflowColumnId}`;
  }

  protected isMoving(taskId: number): boolean {
    return this.movingTaskIds().has(taskId);
  }
}
