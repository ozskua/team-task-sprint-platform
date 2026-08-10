import { NgModule } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSidenavModule } from '@angular/material/sidenav';

import { AdminLayout } from './admin-layout/admin-layout';
import { LayoutRoutingModule } from './layout-routing-module';
import { ShellModule } from './shell-module';

@NgModule({
  declarations: [
    AdminLayout
  ],
  imports: [
    MatButtonModule,
    MatIconModule,
    MatSidenavModule,
    // Sidebar and UserMenu moved here so UserLayout can use the same components.
    ShellModule,
    LayoutRoutingModule
  ]
})
export class LayoutModule { }
