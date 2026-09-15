import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { AllSprints } from './all-sprints/all-sprints';

const routes: Routes = [
  { path: '', component: AllSprints, data: { title: 'Sprints' } }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class SprintsRoutingModule { }
