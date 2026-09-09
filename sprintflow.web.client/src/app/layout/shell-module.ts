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

/**
 * The two pieces both shells use. A component can be declared in exactly one
 * NgModule, so AdminLayout and UserLayout share them from here.
 *
 * Deliberately contains no routing: importing a module that calls
 * RouterModule.forChild() would leak its routes into whichever shell imports it.
 * RouterLink and RouterLinkActive are imported as individual directives for the
 * same reason — we need those two, not the whole RouterModule.
 */
@NgModule({
  declarations: [
    Sidebar,
    UserMenu,
    AreaNav,
    // One component for both shells, beside the account menu in each. Admin and Member see the
    // same bell — a notification belongs to a person, not to a role.
    NotificationBell,
    // Opened programmatically by AreaNav, so it only needs declaring, not exporting.
    AreaInfoDialog,
    // Same: opened by UserMenu. Declared here so both shells' menus can reach it, which is what
    // makes inviting available to Admin and Member alike.
    InviteDialog
  ],
  imports: [
    DatePipe,
    // The unread count on the bell; the only Material module this shell gained for notifications.
    MatBadgeModule,
    MatButtonModule,
    // The only Material module this shell gained for Areas; the other three were already here.
    MatDialogModule,
    MatIconModule,
    MatMenuModule,
    // For the invite dialog, while its link is being created.
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
