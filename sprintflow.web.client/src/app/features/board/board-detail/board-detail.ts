import { CdkDragDrop, moveItemInArray } from '@angular/cdk/drag-drop';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRoute, Router } from '@angular/router';
import { switchMap } from 'rxjs';

import { BoardColumn, BoardTask } from '../../../core/models/board-detail-models';
import { AreaService } from '../../../core/services/area.service';
import { BoardDetailService } from '../../../core/services/board-detail.service';
import { ConfirmDialog, ConfirmDialogData } from '../dialogs/confirm-dialog/confirm-dialog';
import { ColumnFormDialog, ColumnFormDialogData } from '../dialogs/column-form-dialog/column-form-dialog';
import { TaskDetailDialog, TaskDetailDialogResult } from '../dialogs/task-detail-dialog/task-detail-dialog';
import {
  TaskFormDialog,
  TaskFormDialogData,
  TaskFormDialogResult
} from '../dialogs/task-form-dialog/task-form-dialog';

/**
 * Below this a lane stops being readable, so the workflow area starts scrolling sideways instead of
 * squeezing further. Kept beside the gap because the band's width is worked out from both.
 */
const LANE_MIN_WIDTH = 260;
const LANE_GAP = 12;

/**
 * The board page. Everything the user does to a board is started here; the columns and cards below
 * only report what was clicked or dragged.
 *
 * Moves are shown before the server has answered, because a card that lags behind the pointer
 * feels broken. Each one keeps the board exactly as it was beforehand, so a failed request puts
 * the card back where it was picked up from rather than leaving a lie on screen.
 */
@Component({
  selector: 'app-board-detail',
  templateUrl: './board-detail.html',
  standalone: false,
  styleUrl: './board-detail.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BoardDetail {
  private readonly boardDetail = inject(BoardDetailService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly areaService = inject(AreaService);
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly board = this.boardDetail.board;

  protected readonly loading = signal(true);
  protected readonly failed = signal(false);

  /** A local, generic message. The server's own wording is never shown. */
  protected readonly actionError = signal<string | null>(null);

  /** Cards whose move has been sent but not yet answered. */
  protected readonly movingTaskIds = signal<ReadonlySet<number>>(new Set());

  protected readonly toDoColumn = computed(() =>
    this.board()?.columns.find(column => column.systemKind === 'ToDo'));

  /** The only columns a user may reorder, and the only ones that can be dragged. */
  protected readonly customColumns = computed<BoardColumn[]>(() =>
    (this.board()?.columns ?? [])
      .filter(column => column.systemKind === null)
      .slice()
      .sort((left, right) => left.position - right.position));

  /**
   * The two outcome columns, shown stacked in one lane on the right.
   *
   * They stay two separate columns in every sense that matters — their own ids, their own tasks and
   * their own drop lists. Only the lane they are drawn in is shared, because a board rarely needs a
   * full column's width for either of them.
   */
  protected readonly completedColumn = computed(() =>
    this.board()?.columns.find(column => column.systemKind === 'Completed'));

  protected readonly cancelledColumn = computed(() =>
    this.board()?.columns.find(column => column.systemKind === 'Cancelled'));

  protected readonly allColumns = computed(() => this.board()?.columns ?? []);

  /** Every task list can accept a card from every other, including Completed and Cancelled. */
  protected readonly taskListIds = computed<string[]>(() =>
    this.allColumns().map(column => `tasks-${column.workflowColumnId}`));

  /**
   * The custom columns occupy one lane each, so their band claims that many shares of the row and
   * refuses to shrink below the point where a lane stops being readable. Past that the whole
   * workflow area scrolls sideways instead of the lanes overlapping.
   */
  protected readonly customBandFlexGrow = computed(() => this.customColumns().length);

  protected readonly customBandMinWidth = computed(() => {
    const lanes = this.customColumns().length;

    return lanes === 0 ? 0 : lanes * LANE_MIN_WIDTH + (lanes - 1) * LANE_GAP;
  });

  constructor() {
    this.route.paramMap
      .pipe(takeUntilDestroyed())
      .subscribe(params => this.loadBoard(Number(params.get('boardId'))));

    // Dropped on the way out, so opening another board never shows this one's cards first.
    this.destroyRef.onDestroy(() => this.boardDetail.clear());
  }

  protected dismissError(): void {
    this.actionError.set(null);
  }

  protected isMovingTask(taskId: number): boolean {
    return this.movingTaskIds().has(taskId);
  }

  protected canMoveLeft(column: BoardColumn): boolean {
    return this.customColumns().findIndex(c => c.workflowColumnId === column.workflowColumnId) > 0;
  }

  protected canMoveRight(column: BoardColumn): boolean {
    const columns = this.customColumns();

    return columns.findIndex(c => c.workflowColumnId === column.workflowColumnId) < columns.length - 1;
  }

  // --- Tasks --------------------------------------------------------------

  /** The board-level action. Sends no column, so the server starts the task in To Do. */
  protected createTaskInToDo(): void {
    this.openTaskForm({ heading: 'New task', confirmLabel: 'Create' }, result =>
      this.submitCreate(result, undefined));
  }

  /** Opened straight from a card's menu, skipping the details dialog. */
  protected editTaskDirectly(task: BoardTask): void {
    this.editTask(task.taskId);
  }

  protected deleteTaskDirectly(task: BoardTask): void {
    this.confirmDeleteTask(task.taskId);
  }

  /** A column's own action. Sends that column, and the server checks it belongs to this board. */
  protected createTaskInColumn(columnId: number): void {
    const column = this.allColumns().find(c => c.workflowColumnId === columnId);

    this.openTaskForm(
      { heading: `New task in ${column?.name ?? 'this column'}`, confirmLabel: 'Create' },
      result => this.submitCreate(result, columnId));
  }

  protected openTask(task: BoardTask): void {
    const boardId = this.board()?.boardId;

    if (boardId === undefined) {
      return;
    }

    this.dialog
      .open<TaskDetailDialog, unknown, TaskDetailDialogResult>(TaskDetailDialog, {
        data: { taskId: task.taskId },
        autoFocus: 'dialog',
        width: '460px'
      })
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(result => {
        if (result === 'edit') {
          this.editTask(task.taskId);
        } else if (result === 'delete') {
          this.confirmDeleteTask(task.taskId);
        }
      });
  }

  private editTask(taskId: number): void {
    const task = this.findTask(taskId);

    if (task === undefined) {
      return;
    }

    const boardId = this.board()?.boardId;

    if (boardId === undefined) {
      return;
    }

    this.openTaskForm(
      {
        heading: 'Edit task',
        confirmLabel: 'Save',
        title: task.title,
        description: task.description,
        assigneeUserId: task.assignee?.userId ?? null
      },
      // Then re-read the board: changing who a task is assigned to changes who is on the team, and
      // that is the server's answer rather than something to work out here.
      result => this.boardDetail.updateTask(taskId, result)
        .pipe(
          switchMap(() => this.boardDetail.load(boardId)),
          takeUntilDestroyed(this.destroyRef))
        .subscribe({ error: () => this.reportError('That task could not be saved.') }));
  }

  private confirmDeleteTask(taskId: number): void {
    const task = this.findTask(taskId);

    if (task === undefined) {
      return;
    }

    const data: ConfirmDialogData = {
      title: 'Delete this task?',
      message: `"${task.title}" will be removed from the board.`,
      consequence:
        'The task and its participant records are deleted permanently. This cannot be undone, '
        + 'and anyone whose only participation was on this task will no longer be a team member.',
      confirmLabel: 'Delete permanently'
    };

    this.dialog
      .open<ConfirmDialog, ConfirmDialogData, boolean>(ConfirmDialog, { data, autoFocus: 'dialog' })
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(confirmed => {
        if (confirmed === true) {
          this.deleteTask(taskId);
        }
      });
  }

  private deleteTask(taskId: number): void {
    const boardId = this.board()?.boardId;

    if (boardId === undefined) {
      return;
    }

    // Not optimistic: a delete cannot be taken back, so the card stays until the server confirms.
    this.boardDetail.deleteTask(taskId)
      // Then re-read the board. The task's participation rows went with it, so anyone whose only
      // participation was on this task has just stopped being a member — and who is a member is
      // the server's answer, not something to recompute here.
      .pipe(
        switchMap(() => this.boardDetail.load(boardId)),
        takeUntilDestroyed(this.destroyRef))
      .subscribe({ error: () => this.reportError('That task could not be deleted.') });
  }

  // --- Moving -------------------------------------------------------------

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
    // One move at a time per card: a second would be computed against a board the server has not
    // agreed to yet, and the two answers could disagree.
    if (this.movingTaskIds().has(move.taskId)) {
      return;
    }

    const snapshot = this.boardDetail.applyTaskMove(
      move.taskId,
      move.targetColumnId,
      move.targetIndex);

    this.setMoving(move.taskId, true);

    this.boardDetail
      .moveTask(move.taskId, { targetColumnId: move.targetColumnId, targetIndex: move.targetIndex })
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

  // --- Columns ------------------------------------------------------------

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

  /**
   * Delete is offered whatever the column holds. Whether it can actually go is explained here, at
   * the moment it is tried — a warning that sits under a column all the time is noise for the many
   * boards where nobody is trying to delete anything.
   */
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
              // The server refuses a column that gained a task since this dialog opened, which is
              // the same rule stated one layer down.
              error: () => this.reportError(
                'That column could not be deleted. It may no longer be empty.')
            });
        }
      });
  }

  // --- The board itself ---------------------------------------------------

  protected renameBoard(): void {
    const board = this.board();

    if (board === undefined || board === null) {
      return;
    }

    const data: ColumnFormDialogData = {
      heading: 'Rename board',
      confirmLabel: 'Save',
      name: board.name
    };

    this.dialog
      .open<ColumnFormDialog, ColumnFormDialogData, string>(ColumnFormDialog, { data, autoFocus: 'dialog' })
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(name => {
        if (name !== undefined) {
          this.boardDetail.renameBoard(board.boardId, { name })
            .pipe(
              // Re-read both the board and the Area list: the name shows in the sidebar too.
              switchMap(() => this.boardDetail.load(board.boardId)),
              switchMap(() => this.areaService.load()),
              takeUntilDestroyed(this.destroyRef))
            .subscribe({ error: () => this.reportError('That board could not be renamed.') });
        }
      });
  }

  protected confirmDeleteBoard(): void {
    const board = this.board();

    if (board === undefined || board === null) {
      return;
    }

    const data: ConfirmDialogData = {
      title: 'Delete this board?',
      message: `"${board.name}" will be removed from its area.`,
      consequence:
        'The board, its team, its workflow columns, every task on it and every task assignment are '
        + 'deleted permanently. This cannot be undone.',
      confirmLabel: 'Delete permanently'
    };

    this.dialog
      .open<ConfirmDialog, ConfirmDialogData, boolean>(ConfirmDialog, { data, autoFocus: 'dialog' })
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(confirmed => {
        if (confirmed === true) {
          this.deleteBoard(board.boardId);
        }
      });
  }

  private deleteBoard(boardId: number): void {
    this.boardDetail.deleteBoard(boardId)
      // The sidebar still lists the board that has just gone, so the Area list is re-read before
      // leaving. Navigating away matters as much as the delete: this page describes something that
      // no longer exists, and its route would 404 on the next load.
      .pipe(
        switchMap(() => this.areaService.load()),
        takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => void this.router.navigateByUrl(this.dashboardOfCurrentShell()),
        error: () => this.reportError('That board could not be deleted.')
      });
  }

  /**
   * Where to go once the board is gone. Worked out from the current URL rather than hard-coded,
   * because the same page is mounted at `/boards/:id` in one shell and `/admin/boards/:id` in the
   * other — and a relative hop would land on the app root, which redirects to the sign-in page.
   */
  private dashboardOfCurrentShell(): string {
    const prefix = this.router.url.split('/boards/')[0];

    return `${prefix}/dashboard`;
  }

  protected onColumnDropped(event: CdkDragDrop<BoardColumn[]>): void {
    if (event.previousIndex === event.currentIndex) {
      return;
    }

    const ordered = this.customColumns().slice();
    moveItemInArray(ordered, event.previousIndex, event.currentIndex);

    this.submitColumnOrder(ordered.map(column => column.workflowColumnId));
  }

  /** The keyboard and menu path to the same result as dragging a column. */
  protected shiftColumn(column: BoardColumn, delta: number): void {
    const ordered = this.customColumns().slice();
    const from = ordered.findIndex(c => c.workflowColumnId === column.workflowColumnId);
    const to = from + delta;

    if (from < 0 || to < 0 || to >= ordered.length) {
      return;
    }

    moveItemInArray(ordered, from, to);

    this.submitColumnOrder(ordered.map(c => c.workflowColumnId));
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

  // --- Plumbing -----------------------------------------------------------

  private loadBoard(boardId: number): void {
    if (!Number.isInteger(boardId) || boardId <= 0) {
      this.loading.set(false);
      this.failed.set(true);

      return;
    }

    this.loading.set(true);
    this.failed.set(false);
    this.actionError.set(null);

    this.boardDetail.load(boardId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.loading.set(false),
        error: () => {
          this.failed.set(true);
          this.loading.set(false);
        }
      });
  }

  private openTaskForm(
    data: TaskFormDialogData,
    onResult: (result: TaskFormDialogResult) => void): void {
    this.dialog
      .open<TaskFormDialog, TaskFormDialogData, TaskFormDialogResult>(TaskFormDialog, {
        data,
        autoFocus: 'dialog',
        width: '460px'
      })
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(result => {
        if (result !== undefined) {
          onResult(result);
        }
      });
  }

  private submitCreate(result: TaskFormDialogResult, columnId: number | undefined): void {
    const boardId = this.board()?.boardId;

    if (boardId === undefined) {
      return;
    }

    // The task added to the board is the one the server returned — never a local stand-in, so the
    // creator's name, the position and the date are real from the first render. The board is then
    // re-read because creating an assigned task can add someone to the team.
    this.boardDetail.createTask(boardId, { ...result, columnId })
      .pipe(
        switchMap(() => this.boardDetail.load(boardId)),
        takeUntilDestroyed(this.destroyRef))
      .subscribe({ error: () => this.reportError('That task could not be created.') });
  }

  private findTask(taskId: number): BoardTask | undefined {
    return this.board()
      ?.columns
      .flatMap(column => column.tasks)
      .find(task => task.taskId === taskId);
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
