import { NgModule } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSidenavModule } from '@angular/material/sidenav';

import { PlaceholderPage } from '../features/user/placeholder-page';
import { ShellModule } from './shell-module';
import { UserLayout } from './user-layout/user-layout';
import { UserLayoutRoutingModule } from './user-layout-routing-module';

@NgModule({
  declarations: [
    UserLayout,
    PlaceholderPage
  ],
  imports: [
    MatButtonModule,
    MatIconModule,
    MatSidenavModule,
    // The same Sidebar and UserMenu AdminLayout uses.
    ShellModule,
    UserLayoutRoutingModule
  ]
})
export class UserLayoutModule { }
