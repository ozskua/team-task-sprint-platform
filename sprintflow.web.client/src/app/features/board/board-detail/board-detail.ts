import { CdkDragDrop, moveItemInArray } from '@angular/cdk/drag-drop';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRoute } from '@angular/router';

import { Observable, combineLatest } from 'rxjs';

import {
  BoardColumn,
  BoardDetail as BoardDetailModel,
  BoardTask
} from '../../../core/models/board-detail-models';
import { BoardDetailService } from '../../../core/services/board-detail.service';
import { BoardRealtimeService } from '../../../core/services/board-realtime.service';
import { BoardActions } from '../board-actions';
import { ConfirmDialog, ConfirmDialogData } from '../dialogs/confirm-dialog/confirm-dialog';
import { ColumnFormDialog, ColumnFormDialogData } from '../dialogs/column-form-dialog/column-form-dialog';

const LANE_MIN_WIDTH = 260;
const LANE_GAP = 12;

@Component({
  selector: 'app-board-detail',
  templateUrl: './board-detail.html',
  standalone: false,
  styleUrl: './board-detail.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BoardDetail {
  private readonly boardDetail = inject(BoardDetailService);
  private readonly realtime = inject(BoardRealtimeService);
  private readonly boardActions = inject(BoardActions);
  private readonly route = inject(ActivatedRoute);
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly board = this.boardDetail.board;

  protected readonly loading = signal(true);
  protected readonly failed = signal(false);

  protected readonly actionError = signal<string | null>(null);

  protected readonly movingTaskIds = signal<ReadonlySet<number>>(new Set());

  protected readonly toDoColumn = computed(() =>
    this.board()?.columns.find(column => column.systemKind === 'ToDo'));

  protected readonly customColumns = computed<BoardColumn[]>(() =>
    (this.board()?.columns ?? [])
      .filter(column => column.systemKind === null)
      .slice()
      .sort((left, right) => left.position - right.position));

  protected readonly completedColumn = computed(() =>
    this.board()?.columns.find(column => column.systemKind === 'Completed'));

  protected readonly cancelledColumn = computed(() =>
    this.board()?.columns.find(column => column.systemKind === 'Cancelled'));

  protected readonly allColumns = computed(() => this.board()?.columns ?? []);

  protected readonly taskListIds = computed<string[]>(() =>
    this.allColumns().map(column => `tasks-${column.workflowColumnId}`));

  protected readonly customBandFlexGrow = computed(() => this.customColumns().length);

  protected readonly customBandMinWidth = computed(() => {
    const lanes = this.customColumns().length;

    return lanes === 0 ? 0 : lanes * LANE_MIN_WIDTH + (lanes - 1) * LANE_GAP;
  });

  protected readonly selectedSprint = computed(() => this.board()?.selectedSprint ?? null);

  protected readonly hasSprintContext = computed(() => this.selectedSprint() !== null);

  constructor() {
    combineLatest([this.route.paramMap, this.route.queryParamMap])
      .pipe(takeUntilDestroyed())
      .subscribe(([params, query]) => {
        const boardId = Number(params.get('boardId'));

        this.loadBoard(boardId, parseSprintId(query.get('sprint')));

        this.realtime.watch(boardId);
      });

    this.destroyRef.onDestroy(() => {
      this.boardDetail.clear();
      this.realtime.stop();
    });
  }

  protected dismissError(): void {
    this.actionError.set(null);
  }

protected createTaskInToDo(): void {
    this.run(
      board => this.boardActions.createTask(board.boardId),
      'That task could not be created.');
  }

  protected createTaskInColumn(columnId: number): void {
    const column = this.allColumns().find(c => c.workflowColumnId === columnId);

    this.run(
      board => this.boardActions.createTask(board.boardId, columnId, column?.name),
      'That task could not be created.');
  }

  protected openTask(task: BoardTask): void {
    this.run(
      board => this.boardActions.openTaskDetail(board.boardId, task.taskId),
      'That task could not be updated.');
  }

  protected editTaskDirectly(task: BoardTask): void {
    this.run(
      board => this.boardActions.editTask(board.boardId, task.taskId),
      'That task could not be saved.');
  }

  protected deleteTaskDirectly(task: BoardTask): void {
    this.run(
      board => this.boardActions.deleteTask(board.boardId, task.taskId),
      'That task could not be deleted.');
  }

protected onTaskDropped(event: CdkDragDrop<BoardColumn>): void {
    const task = event.item.data as BoardTask;
    const target = event.container.data;
    const source = event.previousContainer.data;

    const unchanged = source.workflowColumnId === target.workflowColumnId
      && event.previousIndex === event.currentIndex;

    if (unchanged) {
      return;
    }

    this.moveTask({
      taskId: task.taskId,
      targetColumnId: target.workflowColumnId,
      targetIndex: event.currentIndex
    });
  }

  protected moveTask(move: { taskId: number; targetColumnId: number; targetIndex: number }): void {
    if (this.movingTaskIds().has(move.taskId)) {
      return;
    }

    const snapshot = this.boardDetail.applyTaskMove(
      move.taskId,
      move.targetColumnId,
      move.targetIndex);

    this.setMoving(move.taskId, true);

    this.boardDetail
      .moveTask(move.taskId, {
        targetColumnId: move.targetColumnId,
        targetIndex: move.targetIndex,
        sprintId: this.boardDetail.scope()
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.setMoving(move.taskId, false),
        error: () => {
          this.boardDetail.restore(snapshot);
          this.setMoving(move.taskId, false);
          this.reportError('That task could not be moved.');
        }
      });
  }

protected createColumn(): void {
    const boardId = this.board()?.boardId;

    if (boardId === undefined) {
      return;
    }

    const data: ColumnFormDialogData = { heading: 'Add column', confirmLabel: 'Create' };

    this.dialog
      .open<ColumnFormDialog, ColumnFormDialogData, string>(ColumnFormDialog, { data, autoFocus: 'dialog' })
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(name => {
        if (name !== undefined) {
          this.boardDetail.createColumn(boardId, { name })
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({ error: () => this.reportError('That column could not be created.') });
        }
      });
  }

  protected renameColumn(column: BoardColumn): void {
    const data: ColumnFormDialogData = {
      heading: 'Rename column',
      confirmLabel: 'Save',
      name: column.name
    };

    this.dialog
      .open<ColumnFormDialog, ColumnFormDialogData, string>(ColumnFormDialog, { data, autoFocus: 'dialog' })
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(name => {
        if (name !== undefined) {
          this.boardDetail.renameColumn(column.workflowColumnId, { name })
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({ error: () => this.reportError('That column could not be renamed.') });
        }
      });
  }

  protected confirmDeleteColumn(column: BoardColumn): void {
    if (column.tasks.length > 0) {
      this.dialog.open<ConfirmDialog, ConfirmDialogData, boolean>(ConfirmDialog, {
        data: {
          title: 'This column still has tasks',
          message: `"${column.name}" holds ${column.tasks.length} task(s), so it cannot be deleted yet.`,
          consequence: 'Move them to another column or delete them first. Nothing is moved for you.',
          confirmLabel: 'Got it',
          acknowledgeOnly: true
        },
        autoFocus: 'dialog'
      });

      return;
    }

    const data: ConfirmDialogData = {
      title: 'Delete this column?',
      message: `"${column.name}" will be removed from this board.`,
      consequence: 'The column is empty, so no task is affected.',
      confirmLabel: 'Delete column'
    };

    this.dialog
      .open<ConfirmDialog, ConfirmDialogData, boolean>(ConfirmDialog, { data, autoFocus: 'dialog' })
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(confirmed => {
        if (confirmed === true) {
          this.boardDetail.deleteColumn(column.workflowColumnId)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
              error: () => this.reportError(
                'That column could not be deleted. It may no longer be empty.')
            });
        }
      });
  }

  protected onColumnDropped(event: CdkDragDrop<BoardColumn[]>): void {
    if (event.previousIndex === event.currentIndex) {
      return;
    }

    const ordered = this.customColumns().slice();
    moveItemInArray(ordered, event.previousIndex, event.currentIndex);

    this.submitColumnOrder(ordered.map(column => column.workflowColumnId));
  }

  private submitColumnOrder(orderedColumnIds: readonly number[]): void {
    const boardId = this.board()?.boardId;

    if (boardId === undefined) {
      return;
    }

    const snapshot = this.boardDetail.applyColumnReorder(orderedColumnIds);

    this.boardDetail.reorderColumns(boardId, { orderedColumnIds })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        error: () => {
          this.boardDetail.restore(snapshot);
          this.reportError('The columns could not be reordered.');
        }
      });
  }

protected renameBoard(): void {
    this.run(board => this.boardActions.renameBoard(board), 'That board could not be renamed.');
  }

  protected confirmDeleteBoard(): void {
    this.run(board => this.boardActions.deleteBoard(board), 'That board could not be deleted.');
  }

private run(
    action: (board: BoardDetailModel) => Observable<unknown>,
    failureMessage: string): void {
    const board = this.board();

    if (board === null) {
      return;
    }

    action(board)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ error: () => this.reportError(failureMessage) });
  }

  private loadBoard(boardId: number, sprintId: number | null): void {
    if (!Number.isInteger(boardId) || boardId <= 0) {
      this.loading.set(false);
      this.failed.set(true);

      return;
    }

    this.loading.set(true);
    this.failed.set(false);
    this.actionError.set(null);

    this.boardDetail.load(boardId, sprintId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.loading.set(false),
        error: () => {
          this.failed.set(true);
          this.loading.set(false);
        }
      });
  }

  private setMoving(taskId: number, moving: boolean): void {
    this.movingTaskIds.update(current => {
      const next = new Set(current);

      if (moving) {
        next.add(taskId);
      } else {
        next.delete(taskId);
      }

      return next;
    });
  }

  private reportError(message: string): void {
    this.actionError.set(message);
  }
}

function parseSprintId(raw: string | null): number | null {
  if (raw === null) {
    return null;
  }

  const parsed = Number(raw);

  return Number.isInteger(parsed) && parsed > 0 ? parsed : null;
}
