import { ChangeDetectionStrategy, Component, computed, inject, input, untracked } from '@angular/core';

import { ProfilePhotoService } from '../../core/services/profile-photo.service';
import { initialsOf } from '../initials';

@Component({
  selector: 'app-avatar',
  templateUrl: './avatar.html',
  standalone: false,
  styleUrl: './avatar.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    role: 'img',
    '[attr.aria-label]': 'ariaLabel()',
    '[attr.title]': 'title() ?? null',
    '[class.avatar--ring]': 'variant() === "ring"',
    '[class.avatar--accent]': 'accent()',
    '[style.width.px]': 'size()',
    '[style.height.px]': 'size()',
    '[style.font-size]': 'fontSize()'
  }
})
export class Avatar {
  private readonly profilePhotos = inject(ProfilePhotoService);

  readonly userId = input<number | null>(null);
  readonly fullName = input.required<string>();
  readonly variant = input<'ring' | 'muted'>('muted');
  readonly accent = input(false);
  readonly size = input(28);
  readonly label = input<string | null>(null);
  readonly title = input<string | null>(null);

  protected readonly initials = computed(() => initialsOf(this.fullName()));

  protected readonly ariaLabel = computed(() => this.label() ?? this.fullName());

  protected readonly fontSize = computed(() => {
    const size = this.size();

    return size <= 28 ? '0.6875rem' : size <= 34 ? '0.75rem' : '1rem';
  });

  protected readonly photoUrl = computed(() => {
    const userId = this.userId();

    if (userId === null) {
      return null;
    }

    const url = untracked(() => this.profilePhotos.urlFor(userId));

    return url();
  });
}
