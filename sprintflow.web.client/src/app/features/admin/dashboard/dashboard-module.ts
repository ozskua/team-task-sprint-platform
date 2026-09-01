import { DatePipe } from '@angular/common';
import { NgModule } from '@angular/core';

import { MetricCardModule } from '../../../shared/metric-card/metric-card-module';
import { AllTasksTable } from './all-tasks-table/all-tasks-table';
import { DashboardRoutingModule } from './dashboard-routing-module';
import { Dashboard } from './dashboard';

/**
 * The admin overview.
 *
 * TrendChart and TeamTable are no longer declared here: the Analytics section is deferred until the
 * project has a model for when work was completed, and Teams is no longer a dashboard section. Their
 * files are left in place, and either is re-declared in one line when its section returns.
 */
@NgModule({
  declarations: [
    Dashboard,
    AllTasksTable
  ],
  imports: [
    // The summary tiles. Declared in their own module now that the board summary draws them too.
    MetricCardModule,
    // The task table's Created column.
    DatePipe,
    // DashboardRoutingModule re-exports RouterModule, which supplies routerLink.
    // Importing RouterModule directly as well trips Angular's duplicate-forRoot guard.
    DashboardRoutingModule
  ]
})
export class DashboardModule { }
