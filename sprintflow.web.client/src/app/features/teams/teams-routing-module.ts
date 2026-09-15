import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { Teams } from './teams';

const routes: Routes = [
  { path: '', component: Teams, data: { title: 'Teams' } }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class TeamsRoutingModule { }
