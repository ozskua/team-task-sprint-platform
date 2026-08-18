import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { UserListItem } from '../../../core/models/user-models';
import { UserService } from '../../../core/services/user.service';

/**
 * Admin view of everyone in the company. Read-only: creating, editing, disabling users and
 * changing roles belong to a later phase.
 *
 * The Role column shows the global role only. It carries no meaning for Areas, Boards, Teams or
 * Tasks — none of which exist yet.
 */
@Component({
  selector: 'app-admin-users',
  templateUrl: './admin-users.html',
  standalone: false,
  styleUrl: './admin-users.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminUsers {
  private readonly userService = inject(UserService);

  protected readonly loading = signal(true);
  protected readonly failed = signal(false);
  protected readonly users = signal<readonly UserListItem[]>([]);
  protected readonly searchTerm = signal('');

  /** Rows drawn while loading, so the table keeps its shape. */
  protected readonly skeletonRows = [0, 1, 2, 3, 4];

  protected readonly visibleUsers = computed(() => {
    const term = AdminUsers.normalize(this.searchTerm());

    if (term.length === 0) {
      return this.users();
    }

    return this.users().filter(user =>
      AdminUsers.normalize(user.fullName).includes(term)
      || AdminUsers.normalize(user.email).includes(term));
  });

  /** Distinguishes "nobody here" from "nothing matched what you typed". */
  protected readonly noMatches = computed(() =>
    this.users().length > 0 && this.visibleUsers().length === 0);

  constructor() {
    this.userService.getAll()
      .pipe(takeUntilDestroyed())
      .subscribe({
        next: users => {
          this.users.set(users);
          this.loading.set(false);
        },
        error: () => {
          // The message is a local constant; the server's text is never shown.
          this.failed.set(true);
          this.loading.set(false);
        }
      });
  }

  protected onSearch(term: string): void {
    this.searchTerm.set(term);
  }

  /** "Ayşe Demir" -> "AD". Purely visual, derived from the name already shown. */
  protected initialsOf(user: UserListItem): string {
    return user.fullName
      .split(' ')
      .filter(part => part.length > 0)
      .slice(0, 2)
      .map(part => part.charAt(0).toLocaleUpperCase('tr'))
      .join('');
  }

  /**
   * Turkish casing: 'İ' lowercases to 'i' here, where the invariant rule would produce 'i' plus a
   * combining dot and stop the search matching.
   */
  private static normalize(value: string): string {
    return value.trim().toLocaleLowerCase('tr');
  }
}
