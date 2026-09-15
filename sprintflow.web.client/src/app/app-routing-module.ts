import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { adminGuard } from './core/guards/admin-guard';
import { authGuard } from './core/guards/auth-guard';

const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  {
    path: 'login',
    loadChildren: () => import('./features/auth/auth-module').then(m => m.AuthModule)
  },
  {
    path: 'register',
    loadChildren: () =>
      import('./features/auth/register/register-module').then(m => m.RegisterModule)
  },
  {
    path: 'admin',
    canActivate: [authGuard, adminGuard],
    loadChildren: () => import('./layout/layout-module').then(m => m.LayoutModule)
  },
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
