import { NgModule } from '@angular/core';

import { Teams } from './teams';
import { TeamsRoutingModule } from './teams-routing-module';

@NgModule({
  declarations: [
    Teams
  ],
  imports: [
    TeamsRoutingModule
  ]
})
export class TeamsModule { }
