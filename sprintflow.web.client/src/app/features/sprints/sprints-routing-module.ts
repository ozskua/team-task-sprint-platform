import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { AllSprints } from './all-sprints/all-sprints';

// Both shells read `title` from the deepest activated route to fill the header.
const routes: Routes = [
  { path: '', component: AllSprints, data: { title: 'Sprints' } }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class SprintsRoutingModule { }
