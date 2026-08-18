import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { AdminLayout } from './admin-layout/admin-layout';

// Feature pages are added as children as each phase lands.
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
      // The same feature module the user shell loads. A board looks and behaves identically in
      // both; only the URL differs, because each shell owns its own prefix.
      //
      // fullBleed tells the shell to hand the page the whole content area: a kanban board is a
      // workspace, not a document, so the centred reading column would waste most of the screen.
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
