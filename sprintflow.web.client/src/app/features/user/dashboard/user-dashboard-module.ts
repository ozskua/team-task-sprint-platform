import { NgModule } from '@angular/core';

import { MyTaskRow } from './my-task-row/my-task-row';
import { StatusChip } from './status-chip/status-chip';
import { TaskCard } from './task-card/task-card';
import { TeamSummaryCard } from './team-summary-card/team-summary-card';
import { UserDashboardRoutingModule } from './user-dashboard-routing-module';
import { UserDashboard } from './user-dashboard';

/**
 * The member's dashboard. RouterModule is not imported directly — the routing module re-exports it,
 * which is what makes the rows' and cards' links work.
 */
@NgModule({
  declarations: [
    UserDashboard,
    MyTaskRow,
    TeamSummaryCard,
    TaskCard,
    StatusChip
  ],
  imports: [
    UserDashboardRoutingModule
  ]
})
export class UserDashboardModule { }
