import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { adminGuard } from './core/guards/admin-guard';
import { authGuard } from './core/guards/auth-guard';

// Guards improve navigation, but do not provide server-side security.
const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  {
    path: 'login',
    loadChildren: () => import('./features/auth/auth-module').then(m => m.AuthModule)
  },
  {
    path: 'admin',
    canActivate: [authGuard, adminGuard], // Signed out goes to /login, a Member goes to /dashboard.
    loadChildren: () => import('./layout/layout-module').then(m => m.LayoutModule)
  },
  // Everything that is not login or admin belongs to the user shell, which keeps
  // /dashboard, /my-teams and /tasks as clean root-level URLs.
  {
    path: '',
    canActivate: [authGuard],
    loadChildren: () => import('./layout/user-layout-module').then(m => m.UserLayoutModule)
  }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
