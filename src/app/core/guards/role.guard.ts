/**
 * Functional Role-Based Access Control Guard
 * Protects routes by comparing user role against expected role in route data
 * Redirects unauthorized users to login route
 */

import { inject } from '@angular/core';
import { CanActivateFn, Router, ActivatedRouteSnapshot } from '@angular/router';
import { AuthService, UserRole } from '../services/auth.service';

/**
 * Guest-only guard for the auth pages (login/register): a user with an
 * active session can't sign in again — they're sent to the home page, and
 * dashboard roles chain-forward from there to their dashboard via
 * dashboardRedirectGuard. Logging out re-opens the auth pages.
 */
export const guestGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return authService.authenticated() ? router.parseUrl('/') : true;
};

/**
 * Sends signed-in dashboard roles (admin, company, broker, maintenance)
 * straight to their dashboard when they hit the public home page.
 * Tenants and owners keep browsing the public site — that IS their surface
 * (tenants use the navbar avatar menu; owners list properties from here).
 */
export const dashboardRedirectGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const role = authService.userRole();

  if (
    authService.authenticated() &&
    role &&
    role !== 'tenant' &&
    role !== 'owner'
  ) {
    return router.parseUrl(authService.dashboardRouteFor(role));
  }
  return true;
};

/**
 * Functional guard factory for role-based route protection
 *
 * Usage in routes:
 * {
 *   path: 'admin',
 *   component: AdminDashboardComponent,
 *   canActivate: [roleGuard],
 *   data: { expectedRole: 'admin' }
 * }
 */
export const roleGuard: CanActivateFn = (
  route: ActivatedRouteSnapshot,
  state
) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // Extract expected role from route data
  const expectedRole = route.data['expectedRole'] as UserRole | undefined;

  // If no expected role is defined, allow access (public route)
  if (!expectedRole) {
    return true;
  }

  // Get current user's role via Signal
  const userRole = authService.userRole();

  // Check if user is authenticated and has required role
  if (authService.authenticated() && userRole === expectedRole) {
    return true;
  }

  // Redirect unauthorized users to login
  router.navigate(['/auth/login'], {
    queryParams: { returnUrl: state.url },
  });

  return false;
};

/**
 * Multi-role guard: user must have at least one of the expected roles
 *
 * Usage in routes:
 * {
 *   path: 'restricted',
 *   component: RestrictedComponent,
 *   canActivate: [multiRoleGuard],
 *   data: { expectedRoles: ['admin', 'broker'] }
 * }
 */
export const multiRoleGuard: CanActivateFn = (
  route: ActivatedRouteSnapshot,
  state
) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // Extract expected roles from route data
  const expectedRoles = route.data['expectedRoles'] as UserRole[] | undefined;

  if (!expectedRoles || expectedRoles.length === 0) {
    return true;
  }

  // Check if user has any of the expected roles
  if (authService.hasAnyRole(...expectedRoles)) {
    return true;
  }

  // Redirect unauthorized users
  router.navigate(['/auth/login'], {
    queryParams: { returnUrl: state.url },
  });

  return false;
};

/**
 * Authentication guard: ensures user is logged in
 * Redirects unauthenticated users to login
 *
 * Usage in routes:
 * {
 *   path: 'protected',
 *   component: ProtectedComponent,
 *   canActivate: [authGuard]
 * }
 */
export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.authenticated()) {
    return true;
  }

  router.navigate(['/auth/login'], {
    queryParams: { returnUrl: state.url },
  });

  return false;
};
