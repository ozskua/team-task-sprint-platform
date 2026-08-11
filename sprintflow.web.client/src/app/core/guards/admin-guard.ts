import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from '../services/auth.service';

/**
 * Reads the `isAdmin` the backend computed at sign-in; no role name is compared here.
 * Runs after authGuard, but repeats the session check so it is also correct on its own.
 */
export const adminGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const allowed = authService.isAuthenticated() && authService.isAdmin();

  return allowed ? true : router.createUrlTree(['/dashboard']);
};
