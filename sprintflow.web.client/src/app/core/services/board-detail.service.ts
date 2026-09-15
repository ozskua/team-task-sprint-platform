import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, switchMap, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  AssignTaskSprintRequest,
  BoardColumn,
  BoardDetail,
  BoardTask,
  ColumnNameRequest,
  CreateTaskCommentRequest,
  CreateTaskRequest,
  MoveTaskRequest,
  ReorderColumnsRequest,
  TaskAttachment,
  TaskComment,
  TaskDetail,
  UpdateTaskRequest
} from '../models/board-detail-models';

@Injectable()
export class BoardDetailService {
  private readonly http = inject(HttpClient);

  private readonly boardState = signal<BoardDetail | null>(null);

  readonly board = this.boardState.asReadonly();

  private readonly scopeState = signal<number | null>(null);

  readonly scope = this.scopeState.asReadonly();

  private readonly allTasksState = signal(false);

  private get baseUrl(): string {
    return `${environment.apiBaseUrl}/boards`;
  }

  load(boardId: number, sprintId?: number | null): Observable<BoardDetail> {
    const options = sprintId == null
      ? {}
      : { params: new HttpParams().set('sprintId', sprintId) };

    return this.http
      .get<BoardDetail>(`${this.baseUrl}/${boardId}`, options)
      .pipe(tap(board => this.store(board, false)));
  }

  loadAllTasks(boardId: number): Observable<BoardDetail> {
    return this.http
      .get<BoardDetail>(`${this.baseUrl}/${boardId}`, {
        params: new HttpParams().set('allTasks', true)
      })
      .pipe(tap(board => this.store(board, true)));
  }

  reload(boardId: number): Observable<BoardDetail> {
    return this.allTasksState()
      ? this.loadAllTasks(boardId)
      : this.load(boardId, this.scopeState());
  }

  clear(): void {
    this.boardState.set(null);
    this.scopeState.set(null);
    this.allTasksState.set(false);
  }

  private store(board: BoardDetail, allTasks: boolean): void {
    this.boardState.set(board);
    this.scopeState.set(board.selectedSprint?.sprintId ?? null);
    this.allTasksState.set(allTasks);
  }

  snapshot(): BoardDetail | null {
    return this.boardState();
  }

  restore(snapshot: BoardDetail | null): void {
    this.boardState.set(snapshot);
  }

createTask(boardId: number, request: CreateTaskRequest): Observable<BoardTask> {
    return this.http
      .post<BoardTask>(`${this.baseUrl}/${boardId}/tasks`, request)
      .pipe(tap(task => this.insertTask(task)));
  }

  updateTask(taskId: number, request: UpdateTaskRequest): Observable<BoardTask> {
    return this.http
      .put<BoardTask>(`${environment.apiBaseUrl}/tasks/${taskId}`, request)
      .pipe(tap(task => this.replaceTask(task)));
  }

  deleteTask(taskId: number): Observable<void> {
    return this.http
      .delete<void>(`${environment.apiBaseUrl}/tasks/${taskId}`)
      .pipe(tap(() => this.removeTask(taskId)));
  }

  moveTask(taskId: number, request: MoveTaskRequest): Observable<readonly BoardColumn[]> {
    return this.http
      .put<readonly BoardColumn[]>(`${environment.apiBaseUrl}/tasks/${taskId}/move`, request)
      .pipe(tap(columns => this.replaceColumns(columns)));
  }

  moveTaskAndReload(
    boardId: number,
    taskId: number,
    request: MoveTaskRequest): Observable<BoardDetail> {
    return this.moveTask(taskId, request)
      .pipe(switchMap(() => this.reload(boardId)));
  }

  assignTaskSprint(taskId: number, request: AssignTaskSprintRequest): Observable<BoardTask> {
    return this.http
      .put<BoardTask>(`${environment.apiBaseUrl}/tasks/${taskId}/sprint`, request)
      .pipe(tap(task => this.replaceTask(task)));
  }

taskDetail(taskId: number): Observable<TaskDetail> {
    return this.http.get<TaskDetail>(`${environment.apiBaseUrl}/tasks/${taskId}`);
  }

  addComment(taskId: number, request: CreateTaskCommentRequest): Observable<TaskComment> {
    return this.http.post<TaskComment>(
      `${environment.apiBaseUrl}/tasks/${taskId}/comments`, request);
  }

  deleteComment(taskId: number, commentId: number): Observable<void> {
    return this.http.delete<void>(
      `${environment.apiBaseUrl}/tasks/${taskId}/comments/${commentId}`);
  }

  uploadAttachment(taskId: number, file: File, commentId?: number): Observable<TaskAttachment> {
    const form = new FormData();
    form.append('file', file);

    if (commentId !== undefined) {
      form.append('commentId', String(commentId));
    }

    return this.http.post<TaskAttachment>(
      `${environment.apiBaseUrl}/tasks/${taskId}/attachments`, form);
  }

  downloadAttachment(taskId: number, attachmentId: number): Observable<Blob> {
    return this.http.get(
      `${environment.apiBaseUrl}/tasks/${taskId}/attachments/${attachmentId}`,
      { responseType: 'blob' });
  }

  deleteAttachment(taskId: number, attachmentId: number): Observable<void> {
    return this.http.delete<void>(
      `${environment.apiBaseUrl}/tasks/${taskId}/attachments/${attachmentId}`);
  }

renameBoard(boardId: number, request: ColumnNameRequest): Observable<unknown> {
    return this.http.put(`${this.baseUrl}/${boardId}`, request);
  }

  deleteBoard(boardId: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${boardId}`);
  }

createColumn(boardId: number, request: ColumnNameRequest): Observable<BoardColumn> {
    return this.http
      .post<BoardColumn>(`${this.baseUrl}/${boardId}/columns`, request)
      .pipe(tap(column => this.insertColumn(column)));
  }

  renameColumn(columnId: number, request: ColumnNameRequest): Observable<BoardColumn> {
    return this.http
      .put<BoardColumn>(`${environment.apiBaseUrl}/columns/${columnId}`, request)
      .pipe(tap(column => this.replaceColumns([column])));
  }

  deleteColumn(columnId: number): Observable<void> {
    return this.http
      .delete<void>(`${environment.apiBaseUrl}/columns/${columnId}`)
      .pipe(tap(() => this.removeColumn(columnId)));
  }

  reorderColumns(boardId: number, request: ReorderColumnsRequest): Observable<readonly BoardColumn[]> {
    return this.http
      .put<readonly BoardColumn[]>(`${this.baseUrl}/${boardId}/columns/order`, request)
      .pipe(tap(columns => this.setColumns(columns)));
  }

applyTaskMove(taskId: number, targetColumnId: number, targetIndex: number): BoardDetail | null {
    const previous = this.boardState();

    if (previous === null) {
      return null;
    }

    const moved = previous.columns
      .flatMap(column => column.tasks)
      .find(task => task.taskId === taskId);

    if (moved === undefined) {
      return previous;
    }

    const columns = previous.columns.map(column => {
      const withoutTask = column.tasks.filter(task => task.taskId !== taskId);

      if (column.workflowColumnId !== targetColumnId) {
        return { ...column, tasks: renumber(withoutTask) };
      }

      const index = Math.max(0, Math.min(targetIndex, withoutTask.length));
      const inserted = [...withoutTask];
      inserted.splice(index, 0, { ...moved, taskColumnId: targetColumnId });

      return { ...column, tasks: renumber(inserted) };
    });

    this.boardState.set({ ...previous, columns });

    return previous;
  }

  applyColumnReorder(orderedCustomIds: readonly number[]): BoardDetail | null {
    const previous = this.boardState();

    if (previous === null) {
      return null;
    }

    const byId = new Map(previous.columns.map(column => [column.workflowColumnId, column]));
    const toDo = previous.columns.filter(column => column.systemKind === 'ToDo');
    const tail = previous.columns.filter(
      column => column.systemKind === 'Completed' || column.systemKind === 'Cancelled');

    const customs = orderedCustomIds
      .map(id => byId.get(id))
      .filter((column): column is BoardColumn => column !== undefined);

    const columns = [...toDo, ...customs, ...tail]
      .map((column, index) => ({ ...column, position: index }));

    this.boardState.set({ ...previous, columns });

    return previous;
  }

private insertTask(task: BoardTask): void {
    this.mutate(board => ({
      ...board,
      columns: board.columns.map(column => column.workflowColumnId === task.taskColumnId
        ? { ...column, tasks: [...column.tasks, task] }
        : column)
    }));
  }

  private replaceTask(updated: BoardTask): void {
    this.mutate(board => ({
      ...board,
      columns: board.columns.map(column => ({
        ...column,
        tasks: column.tasks.map(task => task.taskId === updated.taskId ? updated : task)
      }))
    }));
  }

  private removeTask(taskId: number): void {
    this.mutate(board => ({
      ...board,
      columns: board.columns.map(column => ({
        ...column,
        tasks: renumber(column.tasks.filter(task => task.taskId !== taskId))
      }))
    }));
  }

  private insertColumn(created: BoardColumn): void {
    this.mutate(board => {
      const head = board.columns.filter(column => column.systemKind === null || column.systemKind === 'ToDo');
      const tail = board.columns.filter(
        column => column.systemKind === 'Completed' || column.systemKind === 'Cancelled');

      return {
        ...board,
        columns: [...head, created, ...tail].map((column, index) => ({ ...column, position: index }))
      };
    });
  }

  private replaceColumns(updated: readonly BoardColumn[]): void {
    const byId = new Map(updated.map(column => [column.workflowColumnId, column]));

    this.mutate(board => ({
      ...board,
      columns: board.columns.map(column => byId.get(column.workflowColumnId) ?? column)
    }));
  }

  private setColumns(columns: readonly BoardColumn[]): void {
    this.mutate(board => ({ ...board, columns: [...columns] }));
  }

  private removeColumn(columnId: number): void {
    this.mutate(board => ({
      ...board,
      columns: board.columns
        .filter(column => column.workflowColumnId !== columnId)
        .map((column, index) => ({ ...column, position: index }))
    }));
  }

  private mutate(change: (board: BoardDetail) => BoardDetail): void {
    const current = this.boardState();

    if (current !== null) {
      this.boardState.set(change(current));
    }
  }
}

function renumber(tasks: readonly BoardTask[]): readonly BoardTask[] {
  return tasks.map((task, index) => ({ ...task, position: index }));
}
