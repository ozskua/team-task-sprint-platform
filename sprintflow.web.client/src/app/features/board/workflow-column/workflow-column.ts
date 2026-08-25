import { CdkDragDrop } from '@angular/cdk/drag-drop';
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { BoardColumn, BoardTask } from '../../../core/models/board-detail-models';

/**
 * One column and the cards in it.
 *
 * A column is either one of the three the board is required to have or one a user added. That
 * single fact decides everything this component offers: system columns cannot be renamed, deleted
 * or dragged, so they simply have no controls for it.
 */
@Component({
  selector: 'app-workflow-column',
  templateUrl: './workflow-column.html',
  standalone: false,
  styleUrl: './workflow-column.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class WorkflowColumnComponent {
  readonly column = input.required<BoardColumn>();

  /**
   * The other task lists this one accepts cards from. Mutable on purpose: the CDK's own input is
   * typed that way, and a readonly array will not satisfy it.
   */
  readonly connectedTo = input.required<string[]>();

  /** Task ids whose move has not come back yet. */
  readonly movingTaskIds = input.required<ReadonlySet<number>>();

  /**
   * Whether a task can be added to this column.
   *
   * False on a kanban with no sprint running: the new task would belong to no sprint and would not
   * appear here, so offering it would be a trap.
   */
  readonly canAddTask = input(true);

  readonly addTask = output<number>();
  readonly openTask = output<BoardTask>();
  readonly editTask = output<BoardTask>();
  readonly deleteTask = output<BoardTask>();
  readonly taskDropped = output<CdkDragDrop<BoardColumn>>();
  readonly renameColumn = output<BoardColumn>();
  readonly deleteColumn = output<BoardColumn>();

  protected readonly isSystem = computed(() => this.column().systemKind !== null);

  protected readonly dropListId = computed(() => `tasks-${this.column().workflowColumnId}`);

  protected isMoving(taskId: number): boolean {
    return this.movingTaskIds().has(taskId);
  }
}
