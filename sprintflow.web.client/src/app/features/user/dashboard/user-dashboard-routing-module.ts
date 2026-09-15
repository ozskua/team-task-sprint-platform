import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { UserDashboard } from './user-dashboard';

const routes: Routes = [
  { path: '', component: UserDashboard, data: { title: 'Dashboard' } }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class UserDashboardRoutingModule { }
