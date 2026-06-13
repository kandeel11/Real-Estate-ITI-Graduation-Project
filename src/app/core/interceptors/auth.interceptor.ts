/**
 * Auth Interceptor (functional)
 * Attaches the Bearer token to outgoing requests and handles session expiry:
 *
 *  - token already expired before a request → try silent refresh first; if
 *    refresh fails, call sessionExpired() and abort.
 *  - backend replies 401 → try silent refresh → retry original request once →
 *    only then call sessionExpired().
 *  - concurrent 401s while a refresh is in-flight are queued and replayed
 *    with the new token so only one refresh call goes out.
 *  - /auth/* endpoints are exempt so a wrong password doesn't trigger logout.
 */

import { HttpInterceptorFn, HttpErrorResponse, HttpRequest, HttpHandlerFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { BehaviorSubject, filter, switchMap, take, throwError, catchError } from 'rxjs';
import { AuthService } from '../services/auth.service';

// Module-level refresh state — shared across all interceptor invocations
let isRefreshing = false;
const refreshToken$ = new BehaviorSubject<string | null>(null);

function addBearer(req: HttpRequest<unknown>, token: string) {
  return req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
}

function handleRefresh(
  authService: AuthService,
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
) {
  if (!isRefreshing) {
    isRefreshing = true;
    refreshToken$.next(null);

    return authService.refresh().pipe(
      switchMap((newToken) => {
        isRefreshing = false;
        refreshToken$.next(newToken);
        return next(addBearer(req, newToken));
      }),
      catchError((err) => {
        isRefreshing = false;
        refreshToken$.next(null);
        authService.sessionExpired();
        return throwError(() => err);
      })
    );
  }

  // Another request triggered the refresh — wait for it to finish, then retry
  return refreshToken$.pipe(
    filter((token): token is string => token !== null),
    take(1),
    switchMap((token) => next(addBearer(req, token)))
  );
}

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const token = authService.getToken();
  const isAuthEndpoint = req.url.includes('/auth/');

  // Pre-flight expiry check — try to refresh rather than immediately logging out
  if (token && !isAuthEndpoint && authService.isTokenExpired()) {
    return handleRefresh(authService, req, next);
  }

  const authedReq = token ? addBearer(req, token) : req;

  return next(authedReq).pipe(
    catchError((error) => {
      if (
        error instanceof HttpErrorResponse &&
        error.status === 401 &&
        !isAuthEndpoint &&
        authService.authenticated()
      ) {
        return handleRefresh(authService, req, next);
      }
      return throwError(() => error);
    })
  );
};
