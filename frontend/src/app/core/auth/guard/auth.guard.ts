import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';

import { AuthService } from '@core/auth/service/auth.service';

import { Role } from '@core/auth/types/auth.types';

/**
 * Keeps the dashboard behind a signed-in token.
 *
 * A token that will not decode to a role counts as signed out, so a corrupted
 * one sends the user to log in again rather than into a screen whose guards
 * would bounce them straight back out.
 */
export const authGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return !!authService.role() || router.createUrlTree(['/']);
};

/**
 * Mirrors a route's [Authorize(Roles = ...)] using the roles on route data.
 * Anyone the route does not allow goes to a screen their role can open, which
 * has to be one that lets them in or the redirect bounces them again.
 */
export const roleGuard: CanActivateFn = (route) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const role = authService.role();
  const allowed = route.data['roles'] as Role[];

  if (role && allowed.includes(role)) {
    return true;
  }

  return router.createUrlTree(
    [role === 'Doctor' ? '/dashboard/visits' : '/dashboard'],
    { queryParams: { denied: route.routeConfig?.path } },
  );
};
