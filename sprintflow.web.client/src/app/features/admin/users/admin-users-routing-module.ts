import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { AdminUsers } from './admin-users';

const routes: Routes = [
  { path: '', component: AdminUsers, data: { title: 'Users' } }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AdminUsersRoutingModule { }
