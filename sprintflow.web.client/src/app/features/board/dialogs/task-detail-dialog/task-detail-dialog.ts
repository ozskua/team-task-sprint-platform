import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { switchMap } from 'rxjs';

import {
  BoardColumn,
  TaskAttachment,
  TaskComment,
  TaskDetail,
  TaskPriority,
  UpdateTaskRequest
} from '../../../../core/models/board-detail-models';
import { UserListItem } from '../../../../core/models/user-models';
import { BoardDetailService } from '../../../../core/services/board-detail.service';
import { UserService } from '../../../../core/services/user.service';
import { initialsOf } from '../../../../shared/initials';
import { TASK_PRIORITIES, taskPriorityLabel } from '../../task-priority';

export interface TaskDetailDialogData {
  readonly taskId: number;
}

/** What the page should do once this closes. Editing happens here now, so only deletion is left. */
export type TaskDetailDialogResult = 'delete' | undefined;

/** Which fact is open for editing. One at a time — two half-finished edits help nobody. */
type EditableField = 'title' | 'description' | 'assignee' | 'priority' | 'status';

/**
 * The task card. What the task says on the left — description, date and the thread — and who it
 * concerns on the right: assignee, reporter, priority, status.
 *
 * Every editable fact carries its own pencil and saves on its own, so there is no general "edit the
 * task" mode. Each save goes through the endpoint that already owns it: the ordinary task update
 * for text, assignee and priority, and the same move endpoint a drag uses for status. Both write
 * through BoardDetailService, whose board signal the kanban renders — which is why a change here
 * shows up behind the dialog without a reload.
 *
 * Read on its own rather than from the board's state, because the thread is not part of what the
 * board loads for every card on screen.
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
  private readonly userService = inject(UserService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly task = signal<TaskDetail | null>(null);
  protected readonly loading = signal(true);
  protected readonly failed = signal(false);

  /** Every status this task could be given, in the board's own order. */
  protected readonly columns = computed<readonly BoardColumn[]>(() =>
    this.boardDetail.board()?.columns ?? []);

  protected readonly editing = signal<EditableField | null>(null);
  protected readonly saving = signal(false);

  /** A local, generic message. The server's own wording is never shown. */
  protected readonly saveFailed = signal(false);

  protected readonly draftText = signal('');
  protected readonly draftPriority = signal<TaskPriority>(1);
  protected readonly draftAssignee = signal<number | null>(null);
  protected readonly draftColumnId = signal(0);

  protected readonly priorities = TASK_PRIORITIES;
  protected readonly titleMaxLength = 200;
  protected readonly descriptionMaxLength = 2000;

  /** Only active accounts: a task assigned to someone who cannot sign in has nobody behind it. */
  protected readonly assignableUsers = signal<readonly UserListItem[]>([]);

  /** Which outcome the task's column is, or nothing for the columns still in progress. */
  protected readonly currentOutcome = computed<'Completed' | 'Cancelled' | null>(() => {
    const columnId = this.task()?.taskColumnId;
    const kind = this.columns().find(column => column.workflowColumnId === columnId)?.systemKind;

    return kind === 'Completed' || kind === 'Cancelled' ? kind : null;
  });

  constructor() {
    this.load();

    this.userService.getAll()
      .pipe(takeUntilDestroyed())
      .subscribe({
        next: users => this.assignableUsers.set(users.filter(user => user.isActive)),
        // Left empty: the assignee editor says so rather than offering a list that is not there.
        error: () => this.assignableUsers.set([])
      });
  }

  protected priorityLabel(priority: TaskPriority): string {
    return taskPriorityLabel(priority);
  }

  protected initials(fullName: string): string {
    return initialsOf(fullName);
  }

  // --- Inline editing -----------------------------------------------------

  protected isEditing(field: EditableField): boolean {
    return this.editing() === field;
  }

  /** Opens one field, seeded with what the task says today. */
  protected startEdit(field: EditableField): void {
    const task = this.task();

    if (task === null || this.saving()) {
      return;
    }

    this.saveFailed.set(false);
    this.editing.set(field);

    switch (field) {
      case 'title':
        this.draftText.set(task.title);
        break;
      case 'description':
        this.draftText.set(task.description ?? '');
        break;
      case 'priority':
        this.draftPriority.set(task.priority);
        break;
      case 'assignee':
        this.draftAssignee.set(task.assignee?.userId ?? null);
        break;
      case 'status':
        this.draftColumnId.set(task.taskColumnId);
        break;
    }
  }

  protected cancelEdit(): void {
    this.editing.set(null);
    this.saveFailed.set(false);
  }

  protected onDraftText(value: string): void {
    this.draftText.set(value);
  }

  protected onDraftPriority(value: string): void {
    this.draftPriority.set(Number(value) as TaskPriority);
  }

  protected onDraftAssignee(value: string): void {
    // The empty option is Unassigned, which is a real choice rather than "nothing selected".
    this.draftAssignee.set(value === '' ? null : Number(value));
  }

  protected onDraftColumn(value: string): void {
    this.draftColumnId.set(Number(value));
  }

  protected saveTitle(): void {
    const title = this.draftText().trim();

    // Whitespace alone is not a title. The server rejects it too; stopping here saves a round trip.
    if (title.length > 0) {
      this.applyUpdate({ title });
    }
  }

  protected saveDescription(): void {
    const description = this.draftText().trim();

    // Sent as nothing at all rather than an empty string, which is how the server stores it.
    this.applyUpdate({ description: description.length === 0 ? undefined : description });
  }

  protected savePriority(): void {
    this.applyUpdate({ priority: this.draftPriority() });
  }

  /**
   * Who the task is assigned to. The board is re-read afterwards as well, because participation is
   * what team membership is computed from — the same reason the edit dialog already reloads.
   */
  protected saveAssignee(): void {
    this.applyUpdate({ assigneeUserId: this.draftAssignee() }, true);
  }

  /**
   * Status, through the very same move endpoint a drag uses. It reloads the board, so the card
   * leaves its old column and appears in the new one behind this dialog with nothing to refresh.
   */
  protected saveStatus(): void {
    const task = this.task();
    const targetColumnId = this.draftColumnId();
    const target = this.columns().find(column => column.workflowColumnId === targetColumnId);

    if (task === null || target === undefined || this.saving()) {
      return;
    }

    if (targetColumnId === task.taskColumnId) {
      this.cancelEdit();

      return;
    }

    this.saving.set(true);
    this.saveFailed.set(false);

    // Appended to the end of the target column. The server owns the final position and clamps this.
    this.boardDetail
      .moveTaskAndReload(task.boardId, task.taskId, {
        targetColumnId,
        targetIndex: target.tasks.length
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.editing.set(null);
          // The card names its own column, and the move has just changed it.
          this.patch(current => ({
            ...current,
            taskColumnId: targetColumnId,
            columnName: target.name
          }));
        },
        error: () => {
          this.saving.set(false);
          this.saveFailed.set(true);
        }
      });
  }

  /**
   * One update endpoint for every text and metadata change.
   *
   * The request carries the whole task, so anything not being edited is sent as it stands — an
   * update replaces the task rather than patching it, which is why omitting a field would quietly
   * clear it.
   */
  private applyUpdate(patch: Partial<UpdateTaskRequest>, reloadBoard = false): void {
    const task = this.task();

    if (task === null || this.saving()) {
      return;
    }

    const request: UpdateTaskRequest = {
      title: task.title,
      description: task.description ?? undefined,
      priority: task.priority,
      assigneeUserId: task.assignee?.userId ?? null,
      ...patch
    };

    this.saving.set(true);
    this.saveFailed.set(false);

    const write = this.boardDetail.updateTask(task.taskId, request);

    (reloadBoard
      ? write.pipe(switchMap(saved => this.boardDetail.reload(task.boardId).pipe(
        // The reload answers with the board; the saved task is what this card needs.
        switchMap(() => [saved]))))
      : write)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: saved => {
          this.saving.set(false);
          this.editing.set(null);
          this.patch(current => ({
            ...current,
            title: saved.title,
            description: saved.description,
            priority: saved.priority,
            assignee: saved.assignee
          }));
        },
        error: () => {
          this.saving.set(false);
          this.saveFailed.set(true);
        }
      });
  }

  // --- The thread ---------------------------------------------------------

  protected onCommentAdded(comment: TaskComment): void {
    this.patch(task => ({ ...task, comments: [...task.comments, comment] }));
  }

  protected onCommentDeleted(commentId: number): void {
    this.patch(task => ({
      ...task,
      comments: task.comments.filter(comment => comment.taskCommentId !== commentId)
    }));
  }

  /** The file may hang off a comment or off the task; it is dropped from wherever it is. */
  protected onAttachmentDeleted(attachmentId: number): void {
    this.patch(task => ({
      ...task,
      comments: task.comments.map(comment => ({
        ...comment,
        attachments: comment.attachments.filter(a => a.taskAttachmentId !== attachmentId)
      })),
      attachments: task.attachments.filter(
        (a: TaskAttachment) => a.taskAttachmentId !== attachmentId)
    }));
  }

  // --- Closing ------------------------------------------------------------

  protected requestDelete(): void {
    this.dialogRef.close('delete');
  }

  // --- Plumbing -----------------------------------------------------------

  private load(): void {
    this.loading.set(true);
    this.failed.set(false);

    this.boardDetail.taskDetail(this.data.taskId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: task => {
          this.task.set(task);
          this.loading.set(false);
        },
        error: () => {
          this.failed.set(true);
          this.loading.set(false);
        }
      });
  }

  private patch(change: (task: TaskDetail) => TaskDetail): void {
    const current = this.task();

    if (current !== null) {
      this.task.set(change(current));
    }
  }
}
