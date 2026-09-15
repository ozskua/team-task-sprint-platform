import { DatePipe } from '@angular/common';
import { NgModule } from '@angular/core';
import { MatBadgeModule } from '@angular/material/badge';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RouterLink, RouterLinkActive } from '@angular/router';

import { AreaInfoDialog } from './area-info-dialog/area-info-dialog';
import { AreaNav } from './area-nav/area-nav';
import { InviteDialog } from './invite-dialog/invite-dialog';
import { NotificationBell } from './notification-bell/notification-bell';
import { Sidebar } from './sidebar/sidebar';
import { UserMenu } from './user-menu/user-menu';

@NgModule({
  declarations: [
    Sidebar,
    UserMenu,
    AreaNav,
    NotificationBell,
    AreaInfoDialog,
    InviteDialog
  ],
  imports: [
    DatePipe,
    MatBadgeModule,
    MatButtonModule,
    MatDialogModule,
    MatIconModule,
    MatMenuModule,
    MatProgressSpinnerModule,
    RouterLink,
    RouterLinkActive
  ],
  exports: [
    Sidebar,
    UserMenu,
    AreaNav,
    NotificationBell
  ]
})
export class ShellModule { }
