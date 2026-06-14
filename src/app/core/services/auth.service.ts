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
  private useMock = false; // real backend auth

  private logoutTimer: ReturnType<typeof setTimeout> | null = null;

  private readonly currentUser = signal<AuthUser | null>(null);
  private readonly isAuthenticated = signal<boolean>(false);

  readonly user = this.currentUser.asReadonly();
  readonly authenticated = this.isAuthenticated.asReadonly();
  readonly userRole = computed(() => this.currentUser()?.role ?? null);

  constructor() {
    this.initializeAuth();
  }

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

  private scheduleAutoLogout(token: string): void {
    if (this.logoutTimer) clearTimeout(this.logoutTimer);
    const expiresAt = this.getTokenExpiry(token);
    if (expiresAt === null) return;
    const ms = Math.min(Math.max(expiresAt - Date.now(), 0), 0x7fffffff);
    this.logoutTimer = setTimeout(() => this.sessionExpired(), ms);
  }

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

  dashboardRouteFor(role: UserRole | null): string {
    switch (role) {
      case 'admin':
        return '/dashboard/admin/overview';
      case 'company':
        return '/dashboard/company';
      case 'broker':
        return '/dashboard/broker/overview';
      case 'tenant':
        return '/dashboard';
      case 'maintenance-company':
        return '/dashboard/maintenance-company/overview';
      case 'owner':
        return '/dashboard/landlord/overview';
      default:
        return '/';
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
    localStorage.removeItem('refresh_token');
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