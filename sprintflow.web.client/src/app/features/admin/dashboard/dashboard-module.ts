import { DatePipe } from '@angular/common';
import { NgModule } from '@angular/core';

import { AvatarModule } from '../../../shared/avatar/avatar-module';
import { MetricCardModule } from '../../../shared/metric-card/metric-card-module';
import { AllTasksTable } from './all-tasks-table/all-tasks-table';
import { DashboardRoutingModule } from './dashboard-routing-module';
import { Dashboard } from './dashboard';

@NgModule({
  declarations: [
    Dashboard,
    AllTasksTable
  ],
  imports: [
    AvatarModule,
    MetricCardModule,
    DatePipe,
    DashboardRoutingModule
  ]
})
export class DashboardModule { }
