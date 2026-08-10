import { NgModule } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { RouterLink, RouterLinkActive } from '@angular/router';

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
    UserMenu
  ],
  imports: [
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    RouterLink,
    RouterLinkActive
  ],
  exports: [
    Sidebar,
    UserMenu
  ]
})
export class ShellModule { }
