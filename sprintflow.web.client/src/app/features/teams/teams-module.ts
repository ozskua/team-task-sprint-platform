import { NgModule } from '@angular/core';

import { AvatarModule } from '../../shared/avatar/avatar-module';
import { Teams } from './teams';
import { TeamsRoutingModule } from './teams-routing-module';

@NgModule({
  declarations: [
    Teams
  ],
  imports: [
    AvatarModule,
    TeamsRoutingModule
  ]
})
export class TeamsModule { }
