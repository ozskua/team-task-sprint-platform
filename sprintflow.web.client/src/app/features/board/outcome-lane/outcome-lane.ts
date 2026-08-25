import { CdkDragDrop } from '@angular/cdk/drag-drop';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { BoardColumn, BoardTask } from '../../../core/models/board-detail-models';

/**
 * Completed and Cancelled, drawn as one lane.
 *
 * They stay two separate columns in every sense that matters — their own ids, their own tasks and
 * their own drop lists — because that is what the server stores and what the move endpoint expects.
 * Only the way they are drawn is shared: a board almost never needs a full column's width for
 * either, and two half-empty panels side by side read as clutter rather than as an outcome.
 *
 * So the lane has one frame and one heading. Which of the two a card is in is said by the badge on
 * the card itself, not by the region it sits in — the regions are only visible while a card is
 * being dragged, which is the one moment their boundary matters.
 *
 * This is a component of its own rather than a mode on WorkflowColumnComponent: a column always has
 * a name, a count and an add button, and none of those belong here.
 */
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

  /**
   * The other task lists these two accept cards from. Mutable on purpose: the CDK's own input is
   * typed that way, and a readonly array will not satisfy it.
   */
  readonly connectedTo = input.required<string[]>();

  /** Task ids whose move has not come back yet. */
  readonly movingTaskIds = input.required<ReadonlySet<number>>();

  readonly openTask = output<BoardTask>();
  readonly editTask = output<BoardTask>();
  readonly deleteTask = output<BoardTask>();
  readonly taskDropped = output<CdkDragDrop<BoardColumn>>();

  /** The same id scheme every other list uses, so all of them stay connected to each other. */
  protected dropListId(column: BoardColumn): string {
    return `tasks-${column.workflowColumnId}`;
  }

  protected isMoving(taskId: number): boolean {
    return this.movingTaskIds().has(taskId);
  }
}
