import { NgModule } from '@angular/core';

import { Teams } from './teams';
import { TeamsRoutingModule } from './teams-routing-module';

/** Loaded by both shells at their own URL; the page is identical in each. */
@NgModule({
  declarations: [
    Teams
  ],
  imports: [
    TeamsRoutingModule
  ]
})
export class TeamsModule { }
