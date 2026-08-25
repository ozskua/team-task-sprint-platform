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
  CreateTaskRequest,
  MoveTaskRequest,
  ReorderColumnsRequest,
  UpdateTaskRequest
} from '../models/board-detail-models';

/**
 * Owns the board currently on screen. The signal exists so a create, a move or a rename shows up
 * without a reload — the same approach AreaService uses for the Area list.
 *
 * Everything written into the signal comes from a server response. The two exceptions are the
 * optimistic helpers, which apply a drag immediately and hand back the previous state so the
 * caller can put it back if the request fails.
 */
@Injectable()
export class BoardDetailService {
  private readonly http = inject(HttpClient);

  private readonly boardState = signal<BoardDetail | null>(null);

  readonly board = this.boardState.asReadonly();

  /**
   * The sprint the loaded state is actually showing, or `null` when it is showing none.
   *
   * Taken from the **response**, not from what was asked for: the kanban usually names no sprint
   * and lets the server pick the one running today, so the request says nothing about which sprint
   * is on screen. Creating a task and dragging a card both read this, and both would be wrong if it
   * echoed the request instead of the answer.
   */
  private readonly scopeState = signal<number | null>(null);

  readonly scope = this.scopeState.asReadonly();

  /**
   * Whether the loaded state is the board's complete inventory rather than one sprint's tasks.
   *
   * Remembered because a re-read after a write has to happen in the same mode: reloading the
   * backlog as a kanban would empty it down to the active sprint.
   */
  private readonly allTasksState = signal(false);

  private get baseUrl(): string {
    return `${environment.apiBaseUrl}/boards`;
  }

  /**
   * Loads the board for the kanban: one sprint's tasks.
   *
   * Naming a sprint shows that one. Naming none lets the server use whichever sprint is running
   * today — and if none is, the board comes back with its columns and no cards, which is the
   * honest state of a board with nothing in progress.
   *
   * Either way the columns are the same. The sprint is a filter over one kanban, never a second one.
   */
  load(boardId: number, sprintId?: number | null): Observable<BoardDetail> {
    const options = sprintId == null
      ? {}
      : { params: new HttpParams().set('sprintId', sprintId) };

    return this.http
      .get<BoardDetail>(`${this.baseUrl}/${boardId}`, options)
      .pipe(tap(board => this.store(board, false)));
  }

  /**
   * Loads the board for the backlog: every task, whatever sprint it is in or is not.
   *
   * Asked for explicitly rather than by leaving the sprint out, because leaving it out now means
   * "whatever is running today" — the backlog has to say it wants the whole inventory.
   */
  loadAllTasks(boardId: number): Observable<BoardDetail> {
    return this.http
      .get<BoardDetail>(`${this.baseUrl}/${boardId}`, {
        params: new HttpParams().set('allTasks', true)
      })
      .pipe(tap(board => this.store(board, true)));
  }

  /**
   * Re-reads the board the same way it was read before.
   *
   * What every write does after it succeeds. The mode has to be carried over: reloading the backlog
   * as a kanban would cut it down to one sprint, and reloading a kanban as the backlog would fill
   * it with tasks the user had filtered out.
   */
  reload(boardId: number): Observable<BoardDetail> {
    return this.allTasksState()
      ? this.loadAllTasks(boardId)
      : this.load(boardId, this.scopeState());
  }

  /** Dropped when leaving the page, so the next board never flashes the previous one's cards. */
  clear(): void {
    this.boardState.set(null);
    this.scopeState.set(null);
    this.allTasksState.set(false);
  }

  /** The sprint on screen comes from the server's answer, never from what was requested. */
  private store(board: BoardDetail, allTasks: boolean): void {
    this.boardState.set(board);
    this.scopeState.set(board.selectedSprint?.sprintId ?? null);
    this.allTasksState.set(allTasks);
  }

  /** The current state, for a caller that wants to be able to undo what it is about to do. */
  snapshot(): BoardDetail | null {
    return this.boardState();
  }

  /** Puts a snapshot back after a failed request. */
  restore(snapshot: BoardDetail | null): void {
    this.boardState.set(snapshot);
  }

  // --- Tasks --------------------------------------------------------------

  /**
   * Creates a task. `columnId` is left out by the board-level action, which is what tells the
   * server to start it in To Do; the column-level action sends its own id.
   */
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

  /**
   * Sends the move the UI has already shown. The response carries the columns the server actually
   * touched, so the local guess is replaced by the truth rather than trusted.
   */
  moveTask(taskId: number, request: MoveTaskRequest): Observable<readonly BoardColumn[]> {
    return this.http
      .put<readonly BoardColumn[]>(`${environment.apiBaseUrl}/tasks/${taskId}/move`, request)
      .pipe(tap(columns => this.replaceColumns(columns)));
  }

  /**
   * The same move, followed by a re-read of the whole board.
   *
   * For the callers that pick a status from a list rather than dragging a card: there is nothing on
   * screen to roll back, and reloading is the only way to be sure the positions in every other
   * column match what the server decided.
   */
  moveTaskAndReload(
    boardId: number,
    taskId: number,
    request: MoveTaskRequest): Observable<BoardDetail> {
    // Reloaded the same way the page was read, so it comes back showing what it was showing before.
    // The backlog is the main caller here, and it must not come back as a kanban.
    return this.moveTask(taskId, request)
      .pipe(switchMap(() => this.reload(boardId)));
  }

  /**
   * Puts a task into a sprint, or takes it out. The response is the task as the server now has it,
   * which is what replaces the local copy.
   */
  assignTaskSprint(taskId: number, request: AssignTaskSprintRequest): Observable<BoardTask> {
    return this.http
      .put<BoardTask>(`${environment.apiBaseUrl}/tasks/${taskId}/sprint`, request)
      .pipe(tap(task => this.replaceTask(task)));
  }

  // --- Boards -------------------------------------------------------------

  renameBoard(boardId: number, request: ColumnNameRequest): Observable<unknown> {
    return this.http.put(`${this.baseUrl}/${boardId}`, request);
  }

  /**
   * Deletes the board and everything under it. Nothing is patched locally afterwards — the caller
   * navigates away, because the page this state describes no longer exists.
   */
  deleteBoard(boardId: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${boardId}`);
  }

  // --- Columns ------------------------------------------------------------

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

  // --- Optimistic helpers -------------------------------------------------

  /**
   * Shows the move straight away and returns the state as it was, so a failed request can put the
   * card back exactly where the user picked it up from.
   */
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

  /**
   * Shows a custom-column reorder straight away. The system columns are not moved here either —
   * they keep the places the server pins them to.
   */
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

  // --- Local state maintenance -------------------------------------------

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
    // Placed before Completed, which is where the server puts it. Positions are taken from the
    // rebuilt order rather than invented, so the two cannot drift apart.
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

  /** Applies the columns a write returned, leaving every other column untouched. */
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

/** Keeps the local copy's positions contiguous, matching what the server stored. */
function renumber(tasks: readonly BoardTask[]): readonly BoardTask[] {
  return tasks.map((task, index) => ({ ...task, position: index }));
}
