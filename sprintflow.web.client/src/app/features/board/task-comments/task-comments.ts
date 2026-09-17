import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  input,
  output,
  signal
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { of, switchMap } from 'rxjs';

import { TaskAttachment, TaskComment } from '../../../core/models/board-detail-models';
import { AuthService } from '../../../core/services/auth.service';
import { BoardDetailService } from '../../../core/services/board-detail.service';

const ATTACHMENT_REFUSED =
  'Dosya eklenemedi. En fazla 10 MB, ve yalnızca görsel, PDF, Office, metin ve zip dosyaları '
  + 'eklenebilir.';

@Component({
  selector: 'app-task-comments',
  templateUrl: './task-comments.html',
  standalone: false,
  styleUrl: './task-comments.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TaskComments {
  readonly taskId = input.required<number>();
  readonly comments = input.required<readonly TaskComment[]>();

  readonly looseAttachments = input.required<readonly TaskAttachment[]>();

  readonly commentAdded = output<TaskComment>();
  readonly commentDeleted = output<number>();
  readonly attachmentDeleted = output<number>();

  private readonly boardDetail = inject(BoardDetailService);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly text = signal('');
  protected readonly pendingFile = signal<File | null>(null);
  protected readonly saving = signal(false);

  protected readonly failed = signal(false);
  protected readonly actionError = signal<string | null>(null);

  protected readonly busyAttachmentId = signal<number | null>(null);
  protected readonly busyCommentId = signal<number | null>(null);

  protected readonly maxLength = 2000;

  protected readonly canSubmit = computed(() => !this.saving() && this.text().trim().length > 0);

  private readonly ownUserId = this.auth.userId;

  protected canDelete(comment: TaskComment): boolean {
    const own = this.ownUserId();

    return own !== undefined && comment.userId === own;
  }

  protected formatSize(bytes: number): string {
    return bytes < 1024 * 1024
      ? Math.max(1, Math.round(bytes / 1024)) + ' KB'
      : (bytes / (1024 * 1024)).toFixed(1) + ' MB';
  }

protected onInput(value: string): void {
    this.text.set(value);
  }

  protected onEnterKey(event: Event): void {
    event.preventDefault();
    this.submit();
  }

  protected onFilePicked(input: HTMLInputElement): void {
    const file = input.files?.[0] ?? null;

    input.value = '';

    this.pendingFile.set(file);
    this.actionError.set(null);
  }

  protected clearFile(): void {
    this.pendingFile.set(null);
    this.actionError.set(null);
  }

  protected submit(): void {
    const content = this.text().trim();

    if (content.length === 0 || this.saving()) {
      return;
    }

    const file = this.pendingFile();

    this.saving.set(true);
    this.failed.set(false);
    this.actionError.set(null);

    let posted: TaskComment | null = null;

    this.boardDetail.addComment(this.taskId(), { content })
      .pipe(
        switchMap(comment => {
          posted = comment;

          return file === null
            ? of(null)
            : this.boardDetail.uploadAttachment(this.taskId(), file, comment.taskCommentId);
        }),
        takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: attachment => {
          this.saving.set(false);
          this.clearComposer();
          this.commentAdded.emit(this.withAttachment(posted!, attachment));
        },
        error: () => {
          this.saving.set(false);

          if (posted === null) {
            this.failed.set(true);

            return;
          }

          this.clearComposer();
          this.actionError.set(ATTACHMENT_REFUSED);
          this.commentAdded.emit(posted);
        }
      });
  }

protected deleteComment(comment: TaskComment): void {
    if (this.busyCommentId() !== null) {
      return;
    }

    this.busyCommentId.set(comment.taskCommentId);
    this.actionError.set(null);

    this.boardDetail.deleteComment(this.taskId(), comment.taskCommentId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.busyCommentId.set(null);
          this.commentDeleted.emit(comment.taskCommentId);
        },
        error: () => {
          this.busyCommentId.set(null);
          this.actionError.set('Bu yorum silinemedi.');
        }
      });
  }

  protected removeAttachment(attachment: TaskAttachment): void {
    if (this.busyAttachmentId() !== null) {
      return;
    }

    this.busyAttachmentId.set(attachment.taskAttachmentId);
    this.actionError.set(null);

    this.boardDetail.deleteAttachment(this.taskId(), attachment.taskAttachmentId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.busyAttachmentId.set(null);
          this.attachmentDeleted.emit(attachment.taskAttachmentId);
        },
        error: () => {
          this.busyAttachmentId.set(null);
          this.actionError.set('Bu dosya silinemedi.');
        }
      });
  }

  protected download(attachment: TaskAttachment): void {
    if (this.busyAttachmentId() !== null) {
      return;
    }

    this.busyAttachmentId.set(attachment.taskAttachmentId);
    this.actionError.set(null);

    this.boardDetail.downloadAttachment(this.taskId(), attachment.taskAttachmentId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: blob => {
          this.busyAttachmentId.set(null);
          this.saveBlob(blob, attachment.fileName);
        },
        error: () => {
          this.busyAttachmentId.set(null);
          this.actionError.set('Bu dosya indirilemedi.');
        }
      });
  }

private clearComposer(): void {
    this.text.set('');
    this.pendingFile.set(null);
  }

  private withAttachment(comment: TaskComment, attachment: TaskAttachment | null): TaskComment {
    return attachment === null
      ? comment
      : { ...comment, attachments: [...comment.attachments, attachment] };
  }

  private saveBlob(blob: Blob, fileName: string): void {
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');

    link.href = url;
    link.download = fileName;
    link.click();

    URL.revokeObjectURL(url);
  }
}
