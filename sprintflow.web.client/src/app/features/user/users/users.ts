import { ChangeDetectionStrategy, Component, signal } from '@angular/core';

import { UserSummary } from './users-models';

/**
 * Read-only directory of the people in the system, so a user can find and
 * contact colleagues. There are no management controls here — creating,
 * editing, disabling users and changing roles are Admin responsibilities.
 */
@Component({
  selector: 'app-users',
  templateUrl: './users.html',
  standalone: false,
  styleUrl: './users.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class Users {
  /**
   * No API yet, so the page renders its empty state. When the endpoint exists
   * it sets this same signal and nothing else changes.
   */
  protected readonly loading = signal(false);
  protected readonly users = signal<readonly UserSummary[]>([]);

  /** "Özgür Karakaş" -> "ÖK". Purely visual, derived from the name we already show. */
  protected initialsOf(user: UserSummary): string {
    return user.fullName
      .split(' ')
      .filter(part => part.length > 0)
      .slice(0, 2)
      .map(part => part.charAt(0).toUpperCase())
      .join('');
  }
}
