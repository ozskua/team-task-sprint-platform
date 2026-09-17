import { NgModule } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { MetricCardModule } from '../../../shared/metric-card/metric-card-module';
import { TaskTableModule } from '../../../shared/task-table/task-table-module';
import { SprintReportExport } from './sprint-report-export/sprint-report-export';
import { SprintReportRoutingModule } from './sprint-report-routing-module';
import { SprintReport } from './sprint-report';

@NgModule({
  declarations: [
    SprintReport,
    SprintReportExport
  ],
  imports: [
    MetricCardModule,
    TaskTableModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    SprintReportRoutingModule
  ]
})
export class SprintReportModule { }
