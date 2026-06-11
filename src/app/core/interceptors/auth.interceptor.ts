/**
 * Auth Interceptor (functional)
 * Attaches the Bearer token to outgoing requests, and ends the session when
 * the JWT is no longer valid:
 *  - token already expired before a request → log out + redirect immediately
 *  - backend replies 401 to an authenticated request → log out + redirect
 * Auth endpoints (/auth/*) are exempt so a wrong password on login doesn't
 * trigger the session-expired flow.
 */

import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { throwError, catchError } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const token = authService.getToken();
  const isAuthEndpoint = req.url.includes('/auth/');

  // Token already expired — don't bother the backend
  if (token && !isAuthEndpoint && authService.isTokenExpired()) {
    authService.sessionExpired();
    return throwError(
      () =>
        new HttpErrorResponse({
          status: 401,
          statusText: 'Session expired',
          url: req.url,
        })
    );
  }

  const authedReq = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authedReq).pipe(
    catchError((error) => {
      if (
        error instanceof HttpErrorResponse &&
        error.status === 401 &&
        !isAuthEndpoint &&
        authService.authenticated()
      ) {
        authService.sessionExpired();
      }
      return throwError(() => error);
    })
  );
};
