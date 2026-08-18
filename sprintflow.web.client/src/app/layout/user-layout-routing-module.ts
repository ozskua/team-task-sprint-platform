import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { PlaceholderPage } from '../features/user/placeholder-page';
import { UserLayout } from './user-layout/user-layout';

/**
 * Routes for the normal user area. All three sit at the URL root, so the shell
 * wraps /dashboard, /my-teams and /tasks rather than nesting them under a prefix.
 *
 * My Teams and Tasks point at PlaceholderPage for now. When those features are
 * built, only the `component:` line changes to `loadChildren:` — the sidebar,
 * the shell and the URLs stay exactly as they are.
 */
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
      // The same directory the admin shell shows. Everyone sees the same read-only list — it is
      // also what the assignee picker reads, so a Member who could not see it could not assign
      // anyone.
      {
        path: 'users',
        loadChildren: () =>
          import('../features/admin/users/admin-users-module').then(m => m.AdminUsersModule)
      },
      {
        path: 'teams',
        loadChildren: () => import('../features/teams/teams-module').then(m => m.TeamsModule)
      },
      // The same feature module the admin shell loads, at this shell's own prefix.
      {
        path: 'boards/:boardId',
        data: { fullBleed: true },
        loadChildren: () =>
          import('../features/board/board-detail-module').then(m => m.BoardDetailModule)
      },
      // Tasks is still a later phase. PlaceholderPage keeps the navigation honest — the link works
      // and says plainly that the section does not exist yet.
      { path: 'tasks', component: PlaceholderPage, data: { title: 'Tasks' } }
    ]
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class UserLayoutRoutingModule { }
