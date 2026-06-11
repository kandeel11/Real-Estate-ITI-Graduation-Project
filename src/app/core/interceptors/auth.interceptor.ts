/**
 * Auth Interceptor
 * Attaches the Bearer token to outgoing requests, and ends the session when
 * the JWT is no longer valid:
 *  - token already expired before a request → log out + redirect immediately
 *  - backend replies 401 to an authenticated request → log out + redirect
 * Auth endpoints (/auth/*) are exempt so a wrong password on login doesn't
 * trigger the session-expired flow.
 */

import { Injectable, inject } from '@angular/core';
import {
  HttpInterceptor,
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpErrorResponse,
} from '@angular/common/http';
import { Observable, throwError, catchError } from 'rxjs';
import { AuthService } from '../services/auth.service';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  private authService = inject(AuthService);

  intercept(
    request: HttpRequest<unknown>,
    next: HttpHandler
  ): Observable<HttpEvent<unknown>> {
    const token = this.authService.getToken();
    const isAuthEndpoint = request.url.includes('/auth/');

    // Token already expired — don't bother the backend
    if (token && !isAuthEndpoint && this.authService.isTokenExpired()) {
      this.authService.sessionExpired();
      return throwError(
        () =>
          new HttpErrorResponse({
            status: 401,
            statusText: 'Session expired',
            url: request.url,
          })
      );
    }

    if (token) {
      request = request.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`,
        },
      });
    }

    return next.handle(request).pipe(
      catchError((error) => {
        if (
          error instanceof HttpErrorResponse &&
          error.status === 401 &&
          !isAuthEndpoint &&
          this.authService.authenticated()
        ) {
          this.authService.sessionExpired();
        }
        return throwError(() => error);
      })
    );
  }
}
