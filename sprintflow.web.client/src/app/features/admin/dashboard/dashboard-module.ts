import { NgTemplateOutlet } from '@angular/common';
import { NgModule } from '@angular/core';

import { MetricCard } from './components/metric-card/metric-card';
import { TeamTable } from './components/team-table/team-table';
import { TrendChart } from './components/trend-chart/trend-chart';
import { DashboardRoutingModule } from './dashboard-routing-module';
import { Dashboard } from './dashboard';

@NgModule({
  declarations: [
    Dashboard,
    MetricCard,
    TrendChart,
    TeamTable
  ],
  imports: [
    NgTemplateOutlet,
    // DashboardRoutingModule re-exports RouterModule, which supplies routerLink.
    // Importing RouterModule directly as well trips Angular's duplicate-forRoot guard.
    DashboardRoutingModule
  ]
})
export class DashboardModule { }
