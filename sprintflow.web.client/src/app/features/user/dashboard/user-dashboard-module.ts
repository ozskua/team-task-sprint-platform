import { NgModule } from '@angular/core';

import { StatusChip } from './components/status-chip/status-chip';
import { TaskCard } from './components/task-card/task-card';
import { UserDashboardRoutingModule } from './user-dashboard-routing-module';
import { UserDashboard } from './user-dashboard';

@NgModule({
  declarations: [
    UserDashboard,
    TaskCard,
    StatusChip
  ],
  imports: [
    UserDashboardRoutingModule
  ]
})
export class UserDashboardModule { }
