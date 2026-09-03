import { Injectable, inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { Router } from '@angular/router';
import { EMPTY, Observable, filter, switchMap, tap } from 'rxjs';

import { BoardDetail, BoardTask } from '../../core/models/board-detail-models';
import { AreaService } from '../../core/services/area.service';
import { BoardDetailService } from '../../core/services/board-detail.service';
import { ColumnFormDialog, ColumnFormDialogData } from './dialogs/column-form-dialog/column-form-dialog';
import { ConfirmDialog, ConfirmDialogData } from './dialogs/confirm-dialog/confirm-dialog';
import { TaskDetailDialog, TaskDetailDialogData, TaskDetailDialogResult } from './dialogs/task-detail-dialog/task-detail-dialog';
import {
  TaskFormDialog,
  TaskFormDialogData,
  TaskFormDialogResult
} from './dialogs/task-form-dialog/task-form-dialog';

/**
 * The things a user can do to a task or to the board itself, in one place.
 *
 * Both board views offer these: the kanban from its cards and header, the backlog from its rows and
 * the same header. Each one is a dialog, a write and a re-read — identical wherever it is started
 * from, so it lives here rather than being written twice.
 *
 * Every method returns a cold observable the caller subscribes to. Cancelling a dialog completes
 * without emitting, so "the user changed their mind" needs no special case at the call site. The
 * failure message is the caller's, because only the caller knows which of its buttons was pressed.
 *
 * Columns are deliberately absent: only the kanban has any, so sharing them would be pretending.
 */
@Injectable()
export class BoardActions {
  private readonly dialog = inject(MatDialog);
  private readonly boardDetail = inject(BoardDetailService);
  private readonly areaService = inject(AreaService);
  private readonly router = inject(Router);

  // --- Tasks --------------------------------------------------------------

  /**
   * Creates a task. With no column the server starts it in To Do; the kanban's per-column button
   * passes one, and names it so the dialog can say where the task is going.
   */
  createTask(boardId: number, columnId?: number, columnName?: string): Observable<unknown> {
    const heading = columnName === undefined ? 'New task' : `New task in ${columnName}`;

    // Whatever sprint the page is scoped to, so a task added while looking at a sprint belongs to
    // it. Without this the new task would be filtered straight back out and look like it failed.
    const sprintId = this.boardDetail.scope();

    return this.taskForm({ heading, confirmLabel: 'Create' }).pipe(
      switchMap(result => this.boardDetail.createTask(boardId, { ...result, columnId, sprintId })),
      // Re-read: creating an assigned task can put someone on the team, and who is on the team is
      // the server's answer rather than something to work out here.
      switchMap(() => this.boardDetail.reload(boardId)));
  }

  editTask(boardId: number, taskId: number): Observable<unknown> {
    const task = this.findTask(taskId);

    if (task === undefined) {
      return EMPTY;
    }

    return this.taskForm({
      heading: 'Edit task',
      confirmLabel: 'Save',
      title: task.title,
      description: task.description,
      priority: task.priority,
      assigneeUserId: task.assignee?.userId ?? null
    }).pipe(
      switchMap(result => this.boardDetail.updateTask(taskId, result)),
      // Changing who a task is assigned to changes who is on the team.
      switchMap(() => this.boardDetail.reload(boardId)));
  }

  deleteTask(boardId: number, taskId: number): Observable<unknown> {
    const task = this.findTask(taskId);

    if (task === undefined) {
      return EMPTY;
    }

    return this.confirm({
      title: 'Delete this task?',
      message: `"${task.title}" will be removed from the board.`,
      consequence:
        'The task and its participant records are deleted permanently. This cannot be undone, '
        + 'and anyone whose only participation was on this task will no longer be a team member.',
      confirmLabel: 'Delete permanently'
    }).pipe(
      // Not optimistic: a delete cannot be taken back, so the row stays until the server confirms.
      switchMap(() => this.boardDetail.deleteTask(taskId)),
      // The participation rows went with it, so the team list may be shorter now.
      switchMap(() => this.boardDetail.reload(boardId)));
  }

  /**
   * The full task. Everything editable is edited inside it now, so the only thing it can ask the
   * page for on the way out is the deletion, which needs a confirmation this dialog does not own.
   */
  openTaskDetail(boardId: number, taskId: number): Observable<unknown> {
    return this.dialog
      .open<TaskDetailDialog, TaskDetailDialogData, TaskDetailDialogResult>(TaskDetailDialog, {
        data: { taskId },
        autoFocus: 'dialog',
        // Wide and tall enough to fill the board, and capped so it still fits a small window. The
        // height is fixed on purpose: the thread inside scrolls rather than the card growing.
        width: '1100px',
        maxWidth: '95vw',
        height: '80vh'
      })
      .afterClosed()
      .pipe(switchMap(result => result === 'delete'
        ? this.deleteTask(boardId, taskId)
        : EMPTY));
  }

  // --- The board itself ---------------------------------------------------

  renameBoard(board: BoardDetail): Observable<unknown> {
    return this.nameForm({ heading: 'Rename board', confirmLabel: 'Save', name: board.name }).pipe(
      switchMap(name => this.boardDetail.renameBoard(board.boardId, { name })),
      // Both the board and the Area list: the name shows in the sidebar too.
      switchMap(() => this.boardDetail.reload(board.boardId)),
      switchMap(() => this.areaService.load()));
  }

  deleteBoard(board: BoardDetail): Observable<unknown> {
    return this.confirm({
      title: 'Delete this board?',
      message: `"${board.name}" will be removed from its area.`,
      consequence:
        'The board, its team, its workflow columns, every task on it and every task assignment are '
        + 'deleted permanently. This cannot be undone.',
      confirmLabel: 'Delete permanently'
    }).pipe(
      switchMap(() => this.boardDetail.deleteBoard(board.boardId)),
      // The sidebar still lists the board that has just gone.
      switchMap(() => this.areaService.load()),
      // Leaving matters as much as the delete: this page describes something that no longer exists,
      // and its route would fail on the next load.
      tap(() => void this.router.navigateByUrl(this.dashboardOfCurrentShell())));
  }

  // --- Plumbing -----------------------------------------------------------

  private taskForm(data: TaskFormDialogData): Observable<TaskFormDialogResult> {
    return this.dialog
      .open<TaskFormDialog, TaskFormDialogData, TaskFormDialogResult>(TaskFormDialog, {
        data,
        autoFocus: 'dialog',
        width: '460px'
      })
      .afterClosed()
      .pipe(filter((result): result is TaskFormDialogResult => result !== undefined));
  }

  private nameForm(data: ColumnFormDialogData): Observable<string> {
    return this.dialog
      .open<ColumnFormDialog, ColumnFormDialogData, string>(ColumnFormDialog, {
        data,
        autoFocus: 'dialog'
      })
      .afterClosed()
      .pipe(filter((name): name is string => name !== undefined));
  }

  private confirm(data: ConfirmDialogData): Observable<true> {
    return this.dialog
      .open<ConfirmDialog, ConfirmDialogData, boolean>(ConfirmDialog, { data, autoFocus: 'dialog' })
      .afterClosed()
      .pipe(filter((confirmed): confirmed is true => confirmed === true));
  }

  private findTask(taskId: number): BoardTask | undefined {
    return this.boardDetail.board()
      ?.columns
      .flatMap(column => column.tasks)
      .find(task => task.taskId === taskId);
  }

  /**
   * Where to go once the board is gone. Worked out from the current URL rather than hard-coded,
   * because the same pages are mounted under `/boards/...` in one shell and `/admin/boards/...` in
   * the other — and a relative hop would land on the app root, which redirects to the sign-in page.
   */
  private dashboardOfCurrentShell(): string {
    const prefix = this.router.url.split('/boards/')[0];

    return `${prefix}/dashboard`;
  }
}
