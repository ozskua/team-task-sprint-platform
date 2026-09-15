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

@Injectable()
export class BoardActions {
  private readonly dialog = inject(MatDialog);
  private readonly boardDetail = inject(BoardDetailService);
  private readonly areaService = inject(AreaService);
  private readonly router = inject(Router);

createTask(boardId: number, columnId?: number, columnName?: string): Observable<unknown> {
    const heading = columnName === undefined ? 'New task' : `New task in ${columnName}`;

    const sprintId = this.boardDetail.scope();

    return this.taskForm({ heading, confirmLabel: 'Create' }).pipe(
      switchMap(result => this.boardDetail.createTask(boardId, { ...result, columnId, sprintId })),
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
      switchMap(() => this.boardDetail.deleteTask(taskId)),
      switchMap(() => this.boardDetail.reload(boardId)));
  }

  openTaskDetail(boardId: number, taskId: number): Observable<unknown> {
    return this.dialog
      .open<TaskDetailDialog, TaskDetailDialogData, TaskDetailDialogResult>(TaskDetailDialog, {
        data: { taskId },
        autoFocus: 'dialog',
        width: '1100px',
        maxWidth: '95vw',
        height: '80vh'
      })
      .afterClosed()
      .pipe(switchMap(result => result === 'delete'
        ? this.deleteTask(boardId, taskId)
        : EMPTY));
  }

renameBoard(board: BoardDetail): Observable<unknown> {
    return this.nameForm({ heading: 'Rename board', confirmLabel: 'Save', name: board.name }).pipe(
      switchMap(name => this.boardDetail.renameBoard(board.boardId, { name })),
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
      switchMap(() => this.areaService.load()),
      tap(() => void this.router.navigateByUrl(this.dashboardOfCurrentShell())));
  }

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

  private dashboardOfCurrentShell(): string {
    const prefix = this.router.url.split('/boards/')[0];

    return `${prefix}/dashboard`;
  }
}
