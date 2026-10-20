
import { inject } from '@angular/core';
import {
  CanActivateFn,
  CanActivateChildFn,
  Router
} from '@angular/router';

import { AuthService } from '../services/auth.service';

function checkAccess(url: string) {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) {
    return router.createUrlTree(['/login']);
  }

  // Mandatory first-login password change
  if (
    authService.mustChangePassword() &&
    url.split('?')[0] !== '/change-password'
  ) {
    return router.createUrlTree(['/change-password']);
  }

  return true;
}

export const authGuard: CanActivateFn = (_route, state) =>
  checkAccess(state.url);

export const authChildGuard: CanActivateChildFn = (_route, state) =>
  checkAccess(state.url);
