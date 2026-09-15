import { CdkDragDrop } from '@angular/cdk/drag-drop';
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { BoardColumn, BoardTask } from '../../../core/models/board-detail-models';

@Component({
  selector: 'app-workflow-column',
  templateUrl: './workflow-column.html',
  standalone: false,
  styleUrl: './workflow-column.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class WorkflowColumnComponent {
  readonly column = input.required<BoardColumn>();

  readonly connectedTo = input.required<string[]>();

  readonly movingTaskIds = input.required<ReadonlySet<number>>();

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
