import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { Observable } from 'rxjs';

import {
  BoardColumn,
  BoardDetail as BoardDetailModel,
  BoardTask
} from '../../../core/models/board-detail-models';
import { Sprint } from '../../../core/models/sprint-models';
import { TaskTableRow } from '../../../shared/task-table/task-table';
import { BoardDetailService } from '../../../core/services/board-detail.service';
import { SprintService } from '../../../core/services/sprint.service';
import { BoardActions } from '../board-actions';

interface BacklogRow {
  readonly task: BoardTask;
  readonly column: BoardColumn;
}

@Component({
  selector: 'app-board-backlog',
  templateUrl: './board-backlog.html',
  standalone: false,
  styleUrl: './board-backlog.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BoardBacklog {
  private readonly boardDetail = inject(BoardDetailService);
  private readonly sprintService = inject(SprintService);
  private readonly boardActions = inject(BoardActions);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly board = this.boardDetail.board;

  protected readonly loading = signal(true);
  protected readonly failed = signal(false);
  protected readonly actionError = signal<string | null>(null);

  protected readonly movingTaskIds = signal<ReadonlySet<number>>(new Set());

  protected readonly assigningSprintIds = signal<ReadonlySet<number>>(new Set());

  protected readonly sprints = signal<readonly Sprint[]>([]);
  protected readonly sprintsLoading = signal(true);
  protected readonly sprintsFailed = signal(false);

  protected readonly skeletonRows = [0, 1, 2, 3, 4];

  protected readonly columns = computed<readonly BoardColumn[]>(() => this.board()?.columns ?? []);

  protected readonly rows = computed<readonly BacklogRow[]>(() =>
    this.columns()
      .slice()
      .sort((left, right) => left.position - right.position)
      .flatMap(column => column.tasks
        .slice()
        .sort((left, right) => left.position - right.position)
        .map(task => ({ task, column }))));

  protected readonly taskRows = computed<readonly TaskTableRow[]>(() =>
    this.rows().map(({ task, column }) => ({
      taskId: task.taskId,
      title: task.title,
      columnName: column.name,
      systemKind: column.systemKind,
      assigneeUserId: task.assignee?.userId ?? null,
      assigneeFullName: task.assignee?.fullName ?? null,
      createdByFullName: task.createdByFullName,
      createdDate: task.createdDate
    })));

  private taskById(taskId: number): BoardTask | undefined {
    return this.rows().find(row => row.task.taskId === taskId)?.task;
  }

  protected sprintIdOf(taskId: number): number | null {
    return this.taskById(taskId)?.sprintId ?? null;
  }

  protected columnIdOf(taskId: number): number | undefined {
    return this.taskById(taskId)?.taskColumnId;
  }

  protected openTaskById(taskId: number): void {
    const task = this.taskById(taskId);

    if (task !== undefined) {
      this.openTask(task);
    }
  }

  protected onSprintChangeById(taskId: number, value: string): void {
    const task = this.taskById(taskId);

    if (task !== undefined) {
      this.onSprintChange(task, value);
    }
  }

  protected onStatusChangeById(taskId: number, value: string): void {
    const task = this.taskById(taskId);

    if (task !== undefined) {
      this.onStatusChange(task, value);
    }
  }

  constructor() {
    this.route.paramMap
      .pipe(takeUntilDestroyed())
      .subscribe(params => {
        const boardId = Number(params.get('boardId'));

        this.loadBoard(boardId);
        this.loadSprints(boardId);
      });

    this.destroyRef.onDestroy(() => this.boardDetail.clear());
  }

  protected dismissError(): void {
    this.actionError.set(null);
  }

  protected isMoving(taskId: number): boolean {
    return this.movingTaskIds().has(taskId);
  }

  protected isAssigningSprint(taskId: number): boolean {
    return this.assigningSprintIds().has(taskId);
  }

  protected onSprintChange(task: BoardTask, value: string): void {
    const sprintId = value === '' ? null : Number(value);

    if (sprintId !== null && !Number.isInteger(sprintId)) {
      return;
    }

    if (sprintId === task.sprintId || this.assigningSprintIds().has(task.taskId)) {
      return;
    }

    this.setAssigningSprint(task.taskId, true);

    this.boardDetail.assignTaskSprint(task.taskId, { sprintId })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.setAssigningSprint(task.taskId, false),
        error: () => {
          this.setAssigningSprint(task.taskId, false);
          this.reload();
          this.actionError.set('That task could not be moved to that sprint.');
        }
      });
  }

  protected outcomeOf(column: BoardColumn): 'Completed' | 'Cancelled' | null {
    return column.systemKind === 'Completed' || column.systemKind === 'Cancelled'
      ? column.systemKind
      : null;
  }


protected openTask(task: BoardTask): void {
    this.run(
      board => this.boardActions.openTaskDetail(board.boardId, task.taskId),
      'That task could not be updated.');
  }

  protected createTask(): void {
    this.run(
      board => this.boardActions.createTask(board.boardId),
      'That task could not be created.');
  }

  protected renameBoard(): void {
    this.run(board => this.boardActions.renameBoard(board), 'That board could not be renamed.');
  }

  protected confirmDeleteBoard(): void {
    this.run(board => this.boardActions.deleteBoard(board), 'That board could not be deleted.');
  }

  protected onStatusChange(task: BoardTask, value: string): void {
    const targetColumnId = Number(value);
    const board = this.board();

    if (board === null || !Number.isInteger(targetColumnId)) {
      return;
    }

    if (targetColumnId === task.taskColumnId || this.movingTaskIds().has(task.taskId)) {
      return;
    }

    const target = this.columns().find(column => column.workflowColumnId === targetColumnId);

    if (target === undefined) {
      return;
    }

    this.setMoving(task.taskId, true);

    this.boardDetail
      .moveTaskAndReload(board.boardId, task.taskId, {
        targetColumnId,
        targetIndex: target.tasks.length
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.setMoving(task.taskId, false),
        error: () => {
          this.setMoving(task.taskId, false);
          this.reload();
          this.actionError.set('That task could not be moved.');
        }
      });
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
      .subscribe({ error: () => this.actionError.set(failureMessage) });
  }

  private reload(): void {
    const boardId = this.board()?.boardId;

    if (boardId === undefined) {
      return;
    }

    this.boardDetail.loadAllTasks(boardId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ error: () => undefined });
  }

  private loadBoard(boardId: number): void {
    if (!Number.isInteger(boardId) || boardId <= 0) {
      this.loading.set(false);
      this.failed.set(true);

      return;
    }

    this.loading.set(true);
    this.failed.set(false);
    this.actionError.set(null);

    this.boardDetail.loadAllTasks(boardId)
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
    this.movingTaskIds.update(current => toggle(current, taskId, moving));
  }

  private setAssigningSprint(taskId: number, assigning: boolean): void {
    this.assigningSprintIds.update(current => toggle(current, taskId, assigning));
  }

  private loadSprints(boardId: number): void {
    this.sprintsLoading.set(true);
    this.sprintsFailed.set(false);

    this.sprintService.list(boardId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: sprints => {
          this.sprints.set(sprints);
          this.sprintsLoading.set(false);
        },
        error: () => {
          this.sprintsFailed.set(true);
          this.sprintsLoading.set(false);
        }
      });
  }
}

function toggle(current: ReadonlySet<number>, taskId: number, present: boolean): ReadonlySet<number> {
  const next = new Set(current);

  if (present) {
    next.add(taskId);
  } else {
    next.delete(taskId);
  }

  return next;
}
