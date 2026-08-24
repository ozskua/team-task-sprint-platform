import { DatePipe, NgTemplateOutlet } from '@angular/common';
import { NgModule } from '@angular/core';

import { AllTasksTable } from './components/all-tasks-table/all-tasks-table';
import { MetricCard } from './components/metric-card/metric-card';
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
    MetricCard,
    AllTasksTable
  ],
  imports: [
    // MetricCard renders its body through an ng-template so the card can be a link or a plain div.
    NgTemplateOutlet,
    // The task table's Created column.
    DatePipe,
    // DashboardRoutingModule re-exports RouterModule, which supplies routerLink.
    // Importing RouterModule directly as well trips Angular's duplicate-forRoot guard.
    DashboardRoutingModule
  ]
})
export class DashboardModule { }
