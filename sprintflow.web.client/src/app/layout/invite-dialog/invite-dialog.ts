import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { InvitationService } from '../../core/services/invitation.service';

const UNEXPECTED_ERROR_MESSAGE = 'The invitation could not be created. Please try again.';

@Component({
  selector: 'app-invite-dialog',
  templateUrl: './invite-dialog.html',
  standalone: false,
  styleUrl: './invite-dialog.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class InviteDialog implements OnInit {
  private readonly invitationService = inject(InvitationService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly url = signal<string | null>(null);
  protected readonly expiresAt = signal<string | null>(null);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly copied = signal(false);

  ngOnInit(): void {
    this.invitationService
      .create()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: invitation => {
          this.loading.set(false);
          this.url.set(this.invitationService.registrationUrl(invitation.token));
          this.expiresAt.set(invitation.expiresAt);
        },
        error: () => {
          this.loading.set(false);
          this.errorMessage.set(UNEXPECTED_ERROR_MESSAGE);
        }
      });
  }

  protected async onCopy(): Promise<void> {
    const link = this.url();

    if (!link) {
      return;
    }

    try {
      await navigator.clipboard.writeText(link);
      this.copied.set(true);
    } catch {
      this.copied.set(false);
    }
  }
}
