import { NgModule } from '@angular/core';

import { MyTaskRow } from './my-task-row/my-task-row';
import { StatusChip } from './status-chip/status-chip';
import { TaskCard } from './task-card/task-card';
import { TeamSummaryCard } from './team-summary-card/team-summary-card';
import { UserDashboardRoutingModule } from './user-dashboard-routing-module';
import { UserDashboard } from './user-dashboard';

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
