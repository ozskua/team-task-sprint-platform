import { DatePipe } from '@angular/common';
import { NgModule } from '@angular/core';

import { AdminUsersRoutingModule } from './admin-users-routing-module';
import { AdminUsers } from './admin-users';

// Prefixed with Admin because features/user/users already declares a Users component and module;
// two classes with the same name would be legal but unreadable.
@NgModule({
  declarations: [
    AdminUsers
  ],
  imports: [
    DatePipe,
    AdminUsersRoutingModule
  ]
})
export class AdminUsersModule { }
