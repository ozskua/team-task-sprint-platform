import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';

import { TaskPriority } from '../../../../core/models/board-detail-models';
import { UserListItem } from '../../../../core/models/user-models';
import { UserService } from '../../../../core/services/user.service';
import { DEFAULT_TASK_PRIORITY, TASK_PRIORITIES, taskPriorityLabel } from '../../task-priority';

export interface TaskFormDialogData {
  readonly heading: string;
  readonly confirmLabel: string;

  readonly title?: string;
  readonly description?: string | null;

  readonly priority?: TaskPriority;

  readonly assigneeUserId?: number | null;
}

export interface TaskFormDialogResult {
  readonly title: string;
  readonly description?: string;
  readonly priority: TaskPriority;

  readonly assigneeUserId: number | null;
}

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

  protected readonly assigneeUserId = signal<number | null>(this.data.assigneeUserId ?? null);

  protected readonly assignableUsers = signal<readonly UserListItem[]>([]);
  protected readonly usersLoading = signal(true);
  protected readonly usersFailed = signal(false);

  protected readonly selectedUser = computed(() =>
    this.assignableUsers().find(user => user.userId === this.assigneeUserId()));

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
          this.usersFailed.set(true);
          this.usersLoading.set(false);
        }
      });
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
    this.assigneeUserId.set(value === '' ? null : Number(value));
  }

  protected submit(): void {
    const title = this.title().trim();

    if (title.length === 0) {
      return;
    }

    const description = this.description().trim();

    this.dialogRef.close({
      title,
      description: description.length === 0 ? undefined : description,
      priority: this.priority(),
      assigneeUserId: this.assigneeUserId()
    });
  }
}
