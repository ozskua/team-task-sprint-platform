import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MAT_DIALOG_DATA } from '@angular/material/dialog';

import { Area } from '../../core/models/area-models';
import { AreaService } from '../../core/services/area.service';
import { AuthService } from '../../core/services/auth.service';

const WEBHOOK_LOAD_FAILED_MESSAGE = 'The webhook setting could not be loaded.';
const WEBHOOK_INVALID_MESSAGE = 'Enter an absolute https:// URL of a public host, or leave it empty to disable the webhook.';
const WEBHOOK_SAVE_FAILED_MESSAGE = 'The webhook setting could not be saved. Please try again.';

@Component({
  selector: 'app-area-info-dialog',
  templateUrl: './area-info-dialog.html',
  standalone: false,
  styleUrl: './area-info-dialog.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AreaInfoDialog {
  private readonly areaService = inject(AreaService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly area = inject<Area>(MAT_DIALOG_DATA);

  protected readonly isAdmin = inject(AuthService).isAdmin;

  protected readonly webhookUrl = signal('');
  protected readonly webhookLoaded = signal(false);
  protected readonly saving = signal(false);
  protected readonly saved = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  constructor() {
    if (!this.isAdmin()) {
      return;
    }

    this.areaService.webhook(this.area.areaId)
      .pipe(takeUntilDestroyed())
      .subscribe({
        next: webhook => {
          this.webhookUrl.set(webhook.webhookUrl ?? '');
          this.webhookLoaded.set(true);
        },
        error: () => this.errorMessage.set(WEBHOOK_LOAD_FAILED_MESSAGE)
      });
  }

  protected onWebhookInput(value: string): void {
    this.webhookUrl.set(value);
    this.saved.set(false);
    this.errorMessage.set(null);
  }

  protected onSaveWebhook(): void {
    if (this.saving() || !this.webhookLoaded()) {
      return;
    }

    const url = this.webhookUrl().trim();

    this.saving.set(true);
    this.saved.set(false);
    this.errorMessage.set(null);

    this.areaService.setWebhook(this.area.areaId, url === '' ? null : url)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.saved.set(true);
        },
        error: (error: { status?: number }) => {
          this.saving.set(false);
          this.errorMessage.set(error.status === 400 ? WEBHOOK_INVALID_MESSAGE : WEBHOOK_SAVE_FAILED_MESSAGE);
        }
      });
  }
}
