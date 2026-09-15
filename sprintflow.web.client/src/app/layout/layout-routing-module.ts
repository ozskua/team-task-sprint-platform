import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { AdminLayout } from './admin-layout/admin-layout';

const routes: Routes = [
  {
    path: '',
    component: AdminLayout,
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        loadChildren: () =>
          import('../features/admin/dashboard/dashboard-module').then(m => m.DashboardModule)
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
        path: 'sprint-report',
        loadChildren: () =>
          import('../features/admin/sprint-report/sprint-report-module')
            .then(m => m.SprintReportModule)
      },
      {
        path: 'boards/:boardId',
        data: { fullBleed: true },
        loadChildren: () =>
          import('../features/board/board-detail-module').then(m => m.BoardDetailModule)
      }
    ]
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class LayoutRoutingModule { }
