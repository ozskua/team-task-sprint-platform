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
      {
        path: 'users',
        loadChildren: () =>
          import('../features/user/users/users-module').then(m => m.UsersModule)
      },
      // Teams and Tasks are later phases. PlaceholderPage keeps the navigation
      // honest — the links work and say plainly that the section does not exist yet.
      { path: 'teams', component: PlaceholderPage, data: { title: 'Teams' } },
      { path: 'tasks', component: PlaceholderPage, data: { title: 'Tasks' } }
    ]
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class UserLayoutRoutingModule { }
