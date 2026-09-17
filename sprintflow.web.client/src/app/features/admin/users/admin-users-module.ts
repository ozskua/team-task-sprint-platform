import { DatePipe } from '@angular/common';
import { NgModule } from '@angular/core';

import { AvatarModule } from '../../../shared/avatar/avatar-module';
import { AdminUsersRoutingModule } from './admin-users-routing-module';
import { AdminUsers } from './admin-users';

@NgModule({
  declarations: [
    AdminUsers
  ],
  imports: [
    AvatarModule,
    DatePipe,
    AdminUsersRoutingModule
  ]
})
export class AdminUsersModule { }
