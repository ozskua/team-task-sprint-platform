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

export type TaskDetailDialogResult = 'delete' | undefined;

type EditableField = 'title' | 'description' | 'assignee' | 'priority' | 'status';

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

  protected readonly columns = computed<readonly BoardColumn[]>(() =>
    this.boardDetail.board()?.columns ?? []);

  protected readonly editing = signal<EditableField | null>(null);
  protected readonly saving = signal(false);

  protected readonly saveFailed = signal(false);

  protected readonly draftText = signal('');
  protected readonly draftPriority = signal<TaskPriority>(1);
  protected readonly draftAssignee = signal<number | null>(null);
  protected readonly draftColumnId = signal(0);

  protected readonly priorities = TASK_PRIORITIES;
  protected readonly titleMaxLength = 200;
  protected readonly descriptionMaxLength = 2000;

  protected readonly assignableUsers = signal<readonly UserListItem[]>([]);

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
        error: () => this.assignableUsers.set([])
      });
  }

  protected priorityLabel(priority: TaskPriority): string {
    return taskPriorityLabel(priority);
  }

  protected initials(fullName: string): string {
    return initialsOf(fullName);
  }

protected isEditing(field: EditableField): boolean {
    return this.editing() === field;
  }

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
    this.draftAssignee.set(value === '' ? null : Number(value));
  }

  protected onDraftColumn(value: string): void {
    this.draftColumnId.set(Number(value));
  }

  protected saveTitle(): void {
    const title = this.draftText().trim();

    if (title.length > 0) {
      this.applyUpdate({ title });
    }
  }

  protected saveDescription(): void {
    const description = this.draftText().trim();

    this.applyUpdate({ description: description.length === 0 ? undefined : description });
  }

  protected savePriority(): void {
    this.applyUpdate({ priority: this.draftPriority() });
  }

  protected saveAssignee(): void {
    this.applyUpdate({ assigneeUserId: this.draftAssignee() }, true);
  }

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

protected onCommentAdded(comment: TaskComment): void {
    this.patch(task => ({ ...task, comments: [...task.comments, comment] }));
  }

  protected onCommentDeleted(commentId: number): void {
    this.patch(task => ({
      ...task,
      comments: task.comments.filter(comment => comment.taskCommentId !== commentId)
    }));
  }

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

protected requestDelete(): void {
    this.dialogRef.close('delete');
  }

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
