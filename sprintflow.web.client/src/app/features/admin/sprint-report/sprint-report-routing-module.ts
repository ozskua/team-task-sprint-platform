import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { SprintReport } from './sprint-report';

const routes: Routes = [
  { path: '', component: SprintReport, data: { title: 'Sprint Report' } }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class SprintReportRoutingModule { }
