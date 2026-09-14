import { NgModule } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

import { MetricCardModule } from '../../../shared/metric-card/metric-card-module';
import { TaskTableModule } from '../../../shared/task-table/task-table-module';
import { SprintReportRoutingModule } from './sprint-report-routing-module';
import { SprintReport } from './sprint-report';

@NgModule({
  declarations: [
    SprintReport
  ],
  imports: [
    // The four figures above the table.
    MetricCardModule,
    // The task rows inside an opened sprint — the same table the Backlog draws.
    TaskTableModule,
    // The disclosure chevron.
    MatIconModule,
    // SprintReportRoutingModule re-exports RouterModule, which supplies routerLink.
    SprintReportRoutingModule
  ]
})
export class SprintReportModule { }
