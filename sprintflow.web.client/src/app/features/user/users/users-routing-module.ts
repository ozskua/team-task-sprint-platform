import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { Users } from './users';

// UserLayout reads `title` from the deepest activated route to fill the header.
const routes: Routes = [
  { path: '', component: Users, data: { title: 'Users' } }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class UsersRoutingModule { }
