/**
 * Authentication Service
 * Manages user authentication state via Angular Signals
 * Provides reactive role-based access control
 *
 * Real backend: POST /api/auth/login → { accessToken, refreshToken, expiresAt,
 * user: { id, email, fullName, role, companyId, isEmailVerified } }.
 * Backend role names differ from frontend routes (company_manager → company).
 *
 * `useMock` keeps login usable while the backend is down: any password works
 * and the role is derived from the email's local part — use admin@…,
 * company@…, broker@…, owner@…, anything else signs in as tenant.
 */

import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, of, delay, map, tap, throwError, catchError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { API_ENDPOINTS } from '../constants/api-endpoints';

export type UserRole =
  | 'admin'
  | 'company'
  | 'broker'
  | 'tenant'
  | 'owner'
  | 'maintenance-company';

export interface AuthUser {
  id: string;
  email: string;
  fullName: string;
  role: UserRole;
  companyId: string | null;
  isEmailVerified?: boolean;
}

/** Raw backend login response (role is the backend's name, e.g. company_manager). */
export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: {
    id: string;
    email: string;
    fullName: string;
    role: string;
    companyId: string | null;
    isEmailVerified: boolean;
  };
}

export interface RefreshResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt?: string;
  user?: LoginResponse['user'];
}

const BACKEND_ROLE_MAP: Record<string, UserRole> = {
  admin: 'admin',
  company_manager: 'company',
  broker: 'broker',
  tenant: 'tenant',
  owner: 'owner',
  maintenance_company: 'maintenance-company',
};

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private useMock = false; // ← flip to true for offline demos (no backend)

  private logoutTimer: ReturnType<typeof setTimeout> | null = null;

  private readonly currentUser = signal<AuthUser | null>(null);
  private readonly isAuthenticated = signal<boolean>(false);

  readonly user = this.currentUser.asReadonly();
  readonly authenticated = this.isAuthenticated.asReadonly();
  readonly userRole = computed(() => this.currentUser()?.role ?? null);

  constructor() {
    this.initializeAuth();
  }

  /**
   * Initialize authentication from stored session.
   * A session restored with an already-expired token is discarded so guards
   * send the user straight to login.
   */
  private initializeAuth(): void {
    const storedUser = localStorage.getItem('auth_user');
    if (!storedUser) return;

    if (this.isTokenExpired()) {
      this.clearAuth();
      return;
    }

    try {
      const user = JSON.parse(storedUser) as AuthUser;
      this.currentUser.set(user);
      this.isAuthenticated.set(true);
      const token = this.getToken();
      if (token) this.scheduleAutoLogout(token);
    } catch {
      this.clearAuth();
    }
  }

  // ── JWT expiry handling ─────────────────────────────────────────

  /** Epoch ms of the token's `exp` claim, or null if it can't be decoded
   *  (mock tokens / malformed tokens never expire client-side). */
  private getTokenExpiry(token: string): number | null {
    try {
      const payload = JSON.parse(
        atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/'))
      );
      return typeof payload.exp === 'number' ? payload.exp * 1000 : null;
    } catch {
      return null;
    }
  }

  isTokenExpired(): boolean {
    const token = this.getToken();
    if (!token) return false;
    const expiresAt = this.getTokenExpiry(token);
    return expiresAt !== null && Date.now() >= expiresAt;
  }

  /**
   * Clear the session and send the user to login with a "session expired"
   * notice, remembering where they were so login can return them there.
   */
  sessionExpired(): void {
    const returnUrl = this.router.url;
    this.clearAuth();
    this.router.navigate(['/auth/login'], {
      queryParams: {
        expired: 1,
        ...(returnUrl && returnUrl !== '/' && !returnUrl.startsWith('/auth')
          ? { returnUrl }
          : {}),
      },
    });
  }

  /** Logs out automatically the moment the token expires, even when idle. */
  private scheduleAutoLogout(token: string): void {
    if (this.logoutTimer) clearTimeout(this.logoutTimer);
    const expiresAt = this.getTokenExpiry(token);
    if (expiresAt === null) return;
    const delay = Math.min(Math.max(expiresAt - Date.now(), 0), 0x7fffffff);
    this.logoutTimer = setTimeout(() => this.sessionExpired(), delay);
  }

  /**
   * Authenticate user with credentials.
   * Emits the signed-in user (with the backend role already mapped to the
   * frontend role); auth state and tokens are stored as a side effect.
   */
  login(email: string, password: string): Observable<AuthUser> {
    if (this.useMock) {
      const user = this.mockUserFor(email);
      return of(user).pipe(
        delay(700),
        tap((u) => this.setUser(u, `mock-jwt-${u.role}`))
      );
    }

    return this.http
      .post<LoginResponse>(`${environment.apiUrl}${API_ENDPOINTS.AUTH.LOGIN}`, {
        email,
        password,
      })
      .pipe(
        map((res) => {
          const user = this.toAuthUser(res.user);
          this.setUser(user, res.accessToken, res.refreshToken);
          return user;
        })
      );
  }

  /**
   * Exchange the stored refresh token for a new access token.
   * Updates stored tokens and returns the new access token on success.
   * Clears auth and throws on failure (interceptor calls sessionExpired after).
   */
  refresh(): Observable<string> {
    const refreshToken = localStorage.getItem('auth_refresh_token');
    if (!refreshToken) return throwError(() => new Error('No refresh token stored.'));

    return this.http
      .post<RefreshResponse>(
        `${environment.apiUrl}${API_ENDPOINTS.AUTH.REFRESH_TOKEN}`,
        { refreshToken }
      )
      .pipe(
        tap((res) => {
          const user = res.user ? this.toAuthUser(res.user) : this.user()!;
          this.setUser(user, res.accessToken, res.refreshToken);
        }),
        map((res) => res.accessToken),
        catchError((err) => {
          this.clearAuth();
          return throwError(() => err);
        })
      );
  }

  /** Change the signed-in user's password. */
  changePassword(currentPassword: string, newPassword: string): Observable<void> {
    return this.http.post<void>(
      `${environment.apiUrl}${API_ENDPOINTS.AUTH.CHANGE_PASSWORD}`,
      { currentPassword, newPassword }
    );
  }

  /** Map the backend's user to the frontend AuthUser (role name mapped). */
  toAuthUser(u: LoginResponse['user']): AuthUser {
    return {
      id: u.id,
      email: u.email,
      fullName: u.fullName || u.email,
      role: BACKEND_ROLE_MAP[u.role] ?? 'tenant',
      companyId: u.companyId,
      isEmailVerified: u.isEmailVerified,
    };
  }

  /** Demo user — role derived from the email's local part. */
  private mockUserFor(email: string): AuthUser {
    const local = email.split('@')[0].toLowerCase();
    const role: UserRole = local.includes('admin')
      ? 'admin'
      : local.includes('company')
        ? 'company'
        : local.includes('broker')
          ? 'broker'
          : local.includes('owner')
            ? 'owner'
            : 'tenant';
    return {
      id: 'mock-' + role,
      email,
      fullName: local.charAt(0).toUpperCase() + local.slice(1) + ' (Demo)',
      role,
      companyId: role === 'company' ? 'c1' : null,
    };
  }

  /** Landing route for a role after sign-in. */
  dashboardRouteFor(role: UserRole | null): string {
    switch (role) {
      case 'admin':
        return '/dashboard/admin/overview';
      case 'company':
        return '/dashboard/company';
      case 'broker':
        return '/dashboard/broker/overview';
      case 'tenant':
        return '/dashboard'; // new tenant dashboard mounts at /dashboard root
      case 'maintenance-company':
        return '/dashboard/maintenance-company/overview';
      default:
        return '/'; // owner has no dashboard yet
    }
  }

  setUser(user: AuthUser, accessToken: string, refreshToken?: string): void {
    this.currentUser.set(user);
    this.isAuthenticated.set(true);
    localStorage.setItem('auth_user', JSON.stringify(user));
    localStorage.setItem('auth_token', accessToken);
    if (refreshToken) {
      localStorage.setItem('auth_refresh_token', refreshToken);
    }
    this.scheduleAutoLogout(accessToken);
  }

  logout(): void {
    this.clearAuth();
  }

  private clearAuth(): void {
    if (this.logoutTimer) {
      clearTimeout(this.logoutTimer);
      this.logoutTimer = null;
    }
    this.currentUser.set(null);
    this.isAuthenticated.set(false);
    localStorage.removeItem('auth_user');
    localStorage.removeItem('auth_token');
    localStorage.removeItem('auth_refresh_token');
    localStorage.removeItem('refresh_token'); // legacy key
  }

  getToken(): string | null {
    return localStorage.getItem('auth_token');
  }

  hasRole(role: UserRole): boolean {
    return this.userRole() === role;
  }

  hasAnyRole(...roles: UserRole[]): boolean {
    const currentRole = this.userRole();
    return currentRole ? roles.includes(currentRole) : false;
  }
}
