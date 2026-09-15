import { DatePipe } from '@angular/common';
import { NgModule } from '@angular/core';

import { AdminUsersRoutingModule } from './admin-users-routing-module';
import { AdminUsers } from './admin-users';

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
