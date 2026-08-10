import { NgModule } from '@angular/core';

import { UsersRoutingModule } from './users-routing-module';
import { Users } from './users';

@NgModule({
  declarations: [
    Users
  ],
  imports: [
    UsersRoutingModule
  ]
})
export class UsersModule { }
