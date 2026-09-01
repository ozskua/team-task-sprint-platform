import { ChangeDetectionStrategy, Component, computed, inject, output } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';

import { AuthService } from '../../core/services/auth.service';
import { initialsOf } from '../../shared/initials';
import { InviteDialog } from '../invite-dialog/invite-dialog';

/**
 * The account control in the header, shared by both shells.
 *
 * Shows who is signed in, because two shells that look alike at /dashboard and /admin/dashboard
 * otherwise leave that unanswered. Everything it displays comes from the session the client already
 * holds — `fullName` and `isAdmin`, nothing else. The token is never decoded and no request is made.
 *
 * Signing out is still the layout's job: this only emits, exactly as before, so the clear-then-
 * navigate order the shells rely on is untouched.
 */
@Component({
  selector: 'app-user-menu',
  templateUrl: './user-menu.html',
  standalone: false,
  styleUrl: './user-menu.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UserMenu {
  private readonly authService = inject(AuthService);
  private readonly dialog = inject(MatDialog);

  readonly logout = output<void>();

  protected readonly fullName = this.authService.fullName;

  /**
   * The label for a boolean the server decided. It is named here, not re-derived — nothing reads a
   * role out of the token.
   */
  protected readonly roleLabel = computed(() => this.authService.isAdmin() ? 'Admin' : 'Member');

  /** Initials rather than a photo: the server stores no profile image, and one is not invented. */
  protected readonly initials = computed(() => initialsOf(this.fullName()));

  protected readonly profileLabel = computed(() => `Profile for ${this.fullName()}`);

  protected readonly triggerLabel = computed(() => `Account menu for ${this.fullName()}`);

  /**
   * Settings is reachable but does nothing yet.
   *
   * `[disabled]` would have been the obvious way to grey it out, but MatMenu's key manager skips
   * disabled items, so arrow keys would step straight past it and a keyboard user would never learn
   * it exists. It stays an enabled item, muted by styling only, and this swallows the activation so
   * nothing can follow from it.
   */
  protected onSettingsActivate(event: Event): void {
    event.preventDefault();
    event.stopImmediatePropagation();
  }

  /**
   * Opens the invite dialog, which creates the link itself.
   *
   * No role check, deliberately: a Member may bring someone in exactly as an Admin may, and the
   * server enforces the same — the endpoint requires only that the caller is signed in.
   */
  protected onInvite(): void {
    this.dialog.open(InviteDialog, { autoFocus: false });
  }
}
