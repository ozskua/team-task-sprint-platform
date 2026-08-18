import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';

import { BoardTask } from '../../../../core/models/board-detail-models';
import { BoardDetailService } from '../../../../core/services/board-detail.service';
import { initialsOf } from '../../../../shared/initials';

export interface TaskDetailDialogData {
  readonly taskId: number;
}

/** What the page should do once this closes. */
export type TaskDetailDialogResult = 'edit' | 'delete' | undefined;

/**
 * One task in full: what it says, who wrote it down, when, and who is responsible for it.
 *
 * Read-only. Changing the assignee happens in the edit dialog alongside the title and description,
 * because they are all just fields of the task now — there is no separate membership to manage.
 *
 * The task is read from the board's own state rather than copied in, so an edit made from here is
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

  protected readonly task = computed<BoardTask | undefined>(() =>
    this.boardDetail.board()
      ?.columns
      .flatMap(column => column.tasks)
      .find(candidate => candidate.taskId === this.data.taskId));

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
