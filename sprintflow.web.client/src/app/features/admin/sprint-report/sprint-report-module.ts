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
    MetricCardModule,
    TaskTableModule,
    MatIconModule,
    SprintReportRoutingModule
  ]
})
export class SprintReportModule { }
