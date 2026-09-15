import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { PlaceholderPage } from '../features/user/placeholder-page';
import { UserLayout } from './user-layout/user-layout';

const routes: Routes = [
  {
    path: '',
    component: UserLayout,
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        loadChildren: () =>
          import('../features/user/dashboard/user-dashboard-module').then(m => m.UserDashboardModule)
      },
      {
        path: 'users',
        loadChildren: () =>
          import('../features/admin/users/admin-users-module').then(m => m.AdminUsersModule)
      },
      {
        path: 'teams',
        loadChildren: () => import('../features/teams/teams-module').then(m => m.TeamsModule)
      },
      {
        path: 'sprints',
        loadChildren: () => import('../features/sprints/sprints-module').then(m => m.SprintsModule)
      },
      {
        path: 'boards/:boardId',
        data: { fullBleed: true },
        loadChildren: () =>
          import('../features/board/board-detail-module').then(m => m.BoardDetailModule)
      },
      { path: 'tasks', component: PlaceholderPage, data: { title: 'Tasks' } }
    ]
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class UserLayoutRoutingModule { }
