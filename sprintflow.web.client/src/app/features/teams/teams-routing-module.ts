import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { Teams } from './teams';

// Both shells read `title` from the deepest activated route to fill the header.
const routes: Routes = [
  { path: '', component: Teams, data: { title: 'Teams' } }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class TeamsRoutingModule { }
