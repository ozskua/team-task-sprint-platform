import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { UserListItem } from '../../../core/models/user-models';
import { UserService } from '../../../core/services/user.service';

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
          this.failed.set(true);
          this.loading.set(false);
        }
      });
  }

  protected onSearch(term: string): void {
    this.searchTerm.set(term);
  }

  protected initialsOf(user: UserListItem): string {
    return user.fullName
      .split(' ')
      .filter(part => part.length > 0)
      .slice(0, 2)
      .map(part => part.charAt(0).toLocaleUpperCase('tr'))
      .join('');
  }

  private static normalize(value: string): string {
    return value.trim().toLocaleLowerCase('tr');
  }
}
