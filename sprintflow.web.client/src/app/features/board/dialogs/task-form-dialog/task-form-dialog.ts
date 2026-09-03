import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';

import { TaskPriority } from '../../../../core/models/board-detail-models';
import { UserListItem } from '../../../../core/models/user-models';
import { UserService } from '../../../../core/services/user.service';
import { initialsOf } from '../../../../shared/initials';
import { DEFAULT_TASK_PRIORITY, TASK_PRIORITIES, taskPriorityLabel } from '../../task-priority';

export interface TaskFormDialogData {
  readonly heading: string;
  readonly confirmLabel: string;

  /** Present when editing, absent when creating. */
  readonly title?: string;
  readonly description?: string | null;

  /** Absent when creating, which is what makes a new task Normal. */
  readonly priority?: TaskPriority;

  /** Who the task is assigned to today. `null` — the default for a new task — is unassigned. */
  readonly assigneeUserId?: number | null;
}

/** What the caller gets back. Nothing else — the server decides the rest. */
export interface TaskFormDialogResult {
  readonly title: string;
  readonly description?: string;
  readonly priority: TaskPriority;

  /** `null` means unassigned. */
  readonly assigneeUserId: number | null;
}

/**
 * Writes what a task says and who is responsible for it.
 *
 * There is deliberately no creator field, and no position or column picker. The creator comes from
 * the caller's token, and where a new task lands is decided by which action opened this dialog.
 */
@Component({
  selector: 'app-task-form-dialog',
  templateUrl: './task-form-dialog.html',
  standalone: false,
  styleUrl: './task-form-dialog.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TaskFormDialog {
  protected readonly data = inject<TaskFormDialogData>(MAT_DIALOG_DATA);

  private readonly dialogRef = inject<MatDialogRef<TaskFormDialog, TaskFormDialogResult>>(MatDialogRef);
  private readonly userService = inject(UserService);

  protected readonly title = signal(this.data.title ?? '');
  protected readonly description = signal(this.data.description ?? '');

  protected readonly priority = signal<TaskPriority>(this.data.priority ?? DEFAULT_TASK_PRIORITY);
  protected readonly priorities = TASK_PRIORITIES;

  /** Unassigned unless the task already has someone. */
  protected readonly assigneeUserId = signal<number | null>(this.data.assigneeUserId ?? null);

  /** Only active accounts: a task assigned to someone who cannot sign in has nobody behind it. */
  protected readonly assignableUsers = signal<readonly UserListItem[]>([]);
  protected readonly usersLoading = signal(true);
  protected readonly usersFailed = signal(false);

  /** The chosen person, for the avatar beside the picker. `undefined` while unassigned. */
  protected readonly selectedUser = computed(() =>
    this.assignableUsers().find(user => user.userId === this.assigneeUserId()));

  /** Match the server's column widths, so the form cannot build a request it will reject. */
  protected readonly titleMaxLength = 200;
  protected readonly descriptionMaxLength = 2000;

  constructor() {
    this.userService.getAll()
      .pipe(takeUntilDestroyed())
      .subscribe({
        next: users => {
          this.assignableUsers.set(users.filter(user => user.isActive));
          this.usersLoading.set(false);
        },
        error: () => {
          // The message is a local constant; the server's text is never shown.
          this.usersFailed.set(true);
          this.usersLoading.set(false);
        }
      });
  }

  protected initials(fullName: string): string {
    return initialsOf(fullName);
  }

  protected onTitleInput(value: string): void {
    this.title.set(value);
  }

  protected onDescriptionInput(value: string): void {
    this.description.set(value);
  }

  protected priorityLabel(priority: TaskPriority): string {
    return taskPriorityLabel(priority);
  }

  protected onPriorityChange(value: string): void {
    this.priority.set(Number(value) as TaskPriority);
  }

  protected onAssigneeChange(value: string): void {
    // The empty option is Unassigned, which is a real choice rather than "nothing selected".
    this.assigneeUserId.set(value === '' ? null : Number(value));
  }

  protected submit(): void {
    const title = this.title().trim();

    // Whitespace alone is not a title. The server rejects it too; stopping here saves a round trip.
    if (title.length === 0) {
      return;
    }

    const description = this.description().trim();

    this.dialogRef.close({
      title,
      // Sent as nothing at all rather than an empty string, which is how the server stores it.
      description: description.length === 0 ? undefined : description,
      priority: this.priority(),
      assigneeUserId: this.assigneeUserId()
    });
  }
}
