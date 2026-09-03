import { CdkDragDrop, moveItemInArray } from '@angular/cdk/drag-drop';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRoute } from '@angular/router';

import { Observable, combineLatest } from 'rxjs';

// Aliased: the model and this component are both called BoardDetail.
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

/**
 * Below this a lane stops being readable, so the workflow area starts scrolling sideways instead of
 * squeezing further. Kept beside the gap because the band's width is worked out from both.
 */
const LANE_MIN_WIDTH = 260;
const LANE_GAP = 12;

/**
 * The board page. The columns and cards below only report what was clicked or dragged.
 *
 * Anything a task or the board itself can have done to it lives in BoardActions, because the
 * backlog offers the same things and they must not drift apart. What stays here is what only a
 * kanban has: the columns, and the drag that moves a card between them.
 *
 * Moves are shown before the server has answered, because a card that lags behind the pointer feels
 * broken. Each one keeps the board exactly as it was beforehand, so a failed request puts the card
 * back where it was picked up from rather than leaving a lie on screen.
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
  private readonly realtime = inject(BoardRealtimeService);
  private readonly boardActions = inject(BoardActions);
  private readonly route = inject(ActivatedRoute);
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
   * The two outcome columns, drawn as one lane on the right.
   *
   * They stay two separate columns in every sense that matters — their own ids, their own tasks and
   * their own drop lists. Only the way they are drawn is shared.
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

  /**
   * The sprint whose tasks are on screen, or `null` when none is.
   *
   * Resolved by the server: naming no sprint means "whichever is running today", so this is the
   * answer rather than anything this page worked out.
   */
  protected readonly selectedSprint = computed(() => this.board()?.selectedSprint ?? null);

  /**
   * Whether the board has a sprint in context at all.
   *
   * When it does not, the kanban is deliberately empty — the board's columns are all there with no
   * cards in them — and creating a task is turned off, because a task made here would belong to no
   * sprint and so would not appear in the very view it was created from. The backlog is where a
   * task without a sprint is created.
   */
  protected readonly hasSprintContext = computed(() => this.selectedSprint() !== null);

  constructor() {
    // The board comes from the path and the sprint from the query string, so both have to be
    // watched: switching sprints changes only the latter and would otherwise not reload anything.
    combineLatest([this.route.paramMap, this.route.queryParamMap])
      .pipe(takeUntilDestroyed())
      .subscribe(([params, query]) => {
        const boardId = Number(params.get('boardId'));

        this.loadBoard(boardId, parseSprintId(query.get('sprint')));

        // Here rather than in an init hook: moving to another board only changes the route
        // parameter, so this component is reused and would otherwise stay in the old board's group.
        // Switching sprints repeats the same id, which the service ignores.
        this.realtime.watch(boardId);
      });

    // Dropped on the way out, so opening another board never shows this one's cards first.
    this.destroyRef.onDestroy(() => {
      this.boardDetail.clear();
      this.realtime.stop();
    });
  }

  protected dismissError(): void {
    this.actionError.set(null);
  }

  // --- Tasks --------------------------------------------------------------

  /** The board-level action. Sends no column, so the server starts the task in To Do. */
  protected createTaskInToDo(): void {
    this.run(
      board => this.boardActions.createTask(board.boardId),
      'That task could not be created.');
  }

  /** A column's own action. Sends that column, and the server checks it belongs to this board. */
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

  /** Opened straight from a card's menu, skipping the details dialog. */
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
      .moveTask(move.taskId, {
        targetColumnId: move.targetColumnId,
        targetIndex: move.targetIndex,
        // What the index was counted against. On a scoped board the user only sees this sprint's
        // cards, so the server has to translate the slot back into the whole column.
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

  // --- Columns ------------------------------------------------------------
  // Not shared with the backlog: it has no columns on screen to act on.

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

  /** Dragging the grip is the only way to reorder a column, so this is the only caller. */
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

  // --- The board itself ---------------------------------------------------

  protected renameBoard(): void {
    this.run(board => this.boardActions.renameBoard(board), 'That board could not be renamed.');
  }

  protected confirmDeleteBoard(): void {
    this.run(board => this.boardActions.deleteBoard(board), 'That board could not be deleted.');
  }

  // --- Plumbing -----------------------------------------------------------

  /**
   * Runs one shared action against the board currently loaded, and turns any failure into this
   * page's own wording. The action already knows how to cancel quietly, so there is nothing to
   * handle here but the error.
   */
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
          // Also covers a sprint that is not this board's, which the server refuses rather than
          // answering with an empty board.
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

/**
 * The `sprint` query parameter as a positive integer, or `null` for anything else.
 *
 * A URL can carry anything, so a value that is not a sprint id is treated as no sprint at all —
 * the whole board — rather than being sent on for the server to reject.
 */
function parseSprintId(raw: string | null): number | null {
  if (raw === null) {
    return null;
  }

  const parsed = Number(raw);

  return Number.isInteger(parsed) && parsed > 0 ? parsed : null;
}
