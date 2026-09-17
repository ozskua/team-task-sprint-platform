import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { AuthService } from '../../core/services/auth.service';
import { ProfilePhotoService } from '../../core/services/profile-photo.service';

const MAX_SIZE_BYTES = 2 * 1024 * 1024;
const ACCEPTED_TYPES = ['image/png', 'image/jpeg', 'image/webp'];

const FILE_REFUSED_MESSAGE = 'Choose a PNG, JPEG or WebP image of at most 2 MB.';
const UPLOAD_FAILED_MESSAGE = 'The photo could not be saved. Please try again.';
const REMOVE_FAILED_MESSAGE = 'The photo could not be removed. Please try again.';

@Component({
  selector: 'app-profile-photo-dialog',
  templateUrl: './profile-photo-dialog.html',
  standalone: false,
  styleUrl: './profile-photo-dialog.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProfilePhotoDialog {
  private readonly authService = inject(AuthService);
  private readonly profilePhotos = inject(ProfilePhotoService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly fullName = this.authService.fullName;
  protected readonly userId = computed(() => this.authService.userId() ?? null);

  protected readonly currentUrl = computed(() => {
    const userId = this.userId();

    return userId === null ? null : this.profilePhotos.urlFor(userId)();
  });

  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected onFilePicked(input: HTMLInputElement): void {
    const file = input.files?.[0] ?? null;
    input.value = '';

    this.errorMessage.set(null);

    const userId = this.userId();

    if (file === null || userId === null) {
      return;
    }

    if (file.size > MAX_SIZE_BYTES || !ACCEPTED_TYPES.includes(file.type)) {
      this.errorMessage.set(FILE_REFUSED_MESSAGE);

      return;
    }

    this.saving.set(true);

    this.profilePhotos.upload(userId, file)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.saving.set(false),
        error: () => {
          this.saving.set(false);
          this.errorMessage.set(UPLOAD_FAILED_MESSAGE);
        }
      });
  }

  protected onRemove(): void {
    const userId = this.userId();

    if (userId === null) {
      return;
    }

    this.saving.set(true);
    this.errorMessage.set(null);

    this.profilePhotos.remove(userId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.saving.set(false),
        error: () => {
          this.saving.set(false);
          this.errorMessage.set(REMOVE_FAILED_MESSAGE);
        }
      });
  }
}
