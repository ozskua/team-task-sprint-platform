import { ChangeDetectionStrategy, Component, computed, inject, output } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';

import { AuthService } from '../../core/services/auth.service';
import { InviteDialog } from '../invite-dialog/invite-dialog';
import { ProfilePhotoDialog } from '../profile-photo-dialog/profile-photo-dialog';

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

  protected readonly userId = computed(() => this.authService.userId() ?? null);

  protected readonly roleLabel = computed(() => this.authService.isAdmin() ? 'Admin' : 'Member');

  protected readonly triggerLabel = computed(() => `Account menu for ${this.fullName()}`);

  protected onSettingsActivate(event: Event): void {
    event.preventDefault();
    event.stopImmediatePropagation();
  }

  protected onInvite(): void {
    this.dialog.open(InviteDialog, { autoFocus: false });
  }

  protected onEditPhoto(): void {
    this.dialog.open(ProfilePhotoDialog, { autoFocus: false });
  }
}
