import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';

import { BoardColumn, BoardTask } from '../../../../core/models/board-detail-models';
import { BoardDetailService } from '../../../../core/services/board-detail.service';
import { initialsOf } from '../../../../shared/initials';

export interface TaskDetailDialogData {
  readonly taskId: number;
}

/** What the page should do once this closes. */
export type TaskDetailDialogResult = 'edit' | 'delete' | undefined;

/**
 * One task in full: what it says, where it is in the workflow, who wrote it down, when, and who is
 * responsible for it.
 *
 * The status can be changed from here, because this is the one place that shows every column at
 * once — on the kanban you have to know where a column is before you can drag to it, and two of
 * them share a lane. The move goes through the same endpoint a drag does; the dialog stays open
 * afterwards, since the task on screen is now simply somewhere else.
 *
 * Title, description and assignee are still edited in the edit dialog, where they belong together.
 *
 * The task is read from the board's own state rather than copied in, so a change made from here is
 * reflected without the dialog having to be reopened.
 */
@Component({
  selector: 'app-task-detail-dialog',
  templateUrl: './task-detail-dialog.html',
  standalone: false,
  styleUrl: './task-detail-dialog.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TaskDetailDialog {
  protected readonly data = inject<TaskDetailDialogData>(MAT_DIALOG_DATA);

  private readonly dialogRef = inject<MatDialogRef<TaskDetailDialog, TaskDetailDialogResult>>(MatDialogRef);
  private readonly boardDetail = inject(BoardDetailService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly task = computed<BoardTask | undefined>(() =>
    this.boardDetail.board()
      ?.columns
      .flatMap(column => column.tasks)
      .find(candidate => candidate.taskId === this.data.taskId));

  /** Every status this task could be given, in the board's own order. */
  protected readonly columns = computed<readonly BoardColumn[]>(() =>
    this.boardDetail.board()?.columns ?? []);

  protected readonly currentColumn = computed<BoardColumn | undefined>(() => {
    const columnId = this.task()?.taskColumnId;

    return this.columns().find(column => column.workflowColumnId === columnId);
  });

  protected readonly changingStatus = signal(false);

  /** A local, generic message. The server's own wording is never shown. */
  protected readonly statusFailed = signal(false);

  protected outcomeOf(column: BoardColumn): 'Completed' | 'Cancelled' | null {
    return column.systemKind === 'Completed' || column.systemKind === 'Cancelled'
      ? column.systemKind
      : null;
  }

  protected onStatusChange(value: string): void {
    const targetColumnId = Number(value);
    const task = this.task();
    const boardId = this.boardDetail.board()?.boardId;

    if (task === undefined || boardId === undefined || !Number.isInteger(targetColumnId)) {
      return;
    }

    if (targetColumnId === task.taskColumnId || this.changingStatus()) {
      return;
    }

    const target = this.columns().find(column => column.workflowColumnId === targetColumnId);

    if (target === undefined) {
      return;
    }

    this.changingStatus.set(true);
    this.statusFailed.set(false);

    // Appended to the end of the target column. The server owns the final position and clamps this.
    this.boardDetail
      .moveTaskAndReload(boardId, task.taskId, {
        targetColumnId,
        targetIndex: target.tasks.length
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.changingStatus.set(false),
        error: () => {
          this.changingStatus.set(false);
          this.statusFailed.set(true);
        }
      });
  }

  protected initials(fullName: string): string {
    return initialsOf(fullName);
  }

  protected requestEdit(): void {
    this.dialogRef.close('edit');
  }

  protected requestDelete(): void {
    this.dialogRef.close('delete');
  }
}
