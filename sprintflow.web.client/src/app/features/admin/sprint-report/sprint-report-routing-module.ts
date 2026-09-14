import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { SprintReport } from './sprint-report';

// AdminLayout reads `title` from the deepest activated route to fill the header.
const routes: Routes = [
  { path: '', component: SprintReport, data: { title: 'Sprint Report' } }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class SprintReportRoutingModule { }
