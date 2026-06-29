import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, of, delay, map, tap, throwError } from 'rxjs';
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

export interface RegisterRequest {
  fullName: string;
  email: string;
  phone: string;
  nationalIdNumber: string;
  role: 'owner' | 'broker' | 'tenant';
  password: string;
}

export interface RegisterCompanyRequest {
  adminFullName: string;
  adminEmail: string;
  adminPhone: string;
  password: string;
  companyName: string;
  commercialRegister: string;
  city: string;
  address: string;
  companyPhone: string;
  companyEmail: string;
}

export interface RefreshResponse {
  accessToken: string;
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

  private readonly currentUser      = signal<AuthUser | null>(null);
  private readonly isAuthenticated  = signal<boolean>(false);
  // Access token lives in memory only — never touches localStorage
  private readonly _accessToken     = signal<string | null>(null);

  readonly user          = this.currentUser.asReadonly();
  readonly authenticated = this.isAuthenticated.asReadonly();
  readonly userRole      = computed(() => this.currentUser()?.role ?? null);

  constructor() {
    this.initializeAuth();
  }

  private initializeAuth(): void {
    const storedUser = localStorage.getItem('auth_user');
    if (!storedUser) return;

    try {
      const user = JSON.parse(storedUser) as AuthUser;
      // Restore identity from localStorage for immediate UX
      this.currentUser.set(user);
      this.isAuthenticated.set(true);
      // Silently restore access token from the HttpOnly refresh cookie.
      // On failure: stay authenticated — the interceptor's 401 path will
      // call sessionExpired() and redirect to login when an API call fires.
      this.refresh().subscribe({ error: () => {} });
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
    const token = this._accessToken();
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
      .post<LoginResponse>(
        `${environment.apiUrl}${API_ENDPOINTS.AUTH.LOGIN}`,
        { email, password },
        { withCredentials: true }
      )
      .pipe(
        map((res) => {
          const user = this.toAuthUser(res.user);
          this.setUser(user, res.accessToken);
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
    return this.http
      .post<RefreshResponse>(
        `${environment.apiUrl}${API_ENDPOINTS.AUTH.REFRESH_TOKEN}`,
        {},
        { withCredentials: true }
      )
      .pipe(
        tap((res) => {
          const user = res.user ? this.toAuthUser(res.user) : this.user()!;
          this.setUser(user, res.accessToken);
        }),
        map((res) => res.accessToken)
        // No clearAuth here — the interceptor's handleRefresh calls
        // sessionExpired() on failure, which owns the logout decision.
      );
  }

  register(data: RegisterRequest): Observable<void> {
    return this.http.post<void>(
      `${environment.apiUrl}${API_ENDPOINTS.AUTH.REGISTER}`,
      data
    );
  }

  registerCompany(data: RegisterCompanyRequest): Observable<void> {
    return this.http.post<void>(
      `${environment.apiUrl}${API_ENDPOINTS.AUTH.REGISTER_COMPANY}`,
      data
    );
  }

  /** Resend the email-verification link to the given address (no-op if already verified). */
  resendVerification(email: string): Observable<void> {
    return this.http.post<void>(
      `${environment.apiUrl}${API_ENDPOINTS.AUTH.RESEND_VERIFICATION}`,
      { email }
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
        return '/dashboard/maintenance/overview';
      case 'owner':
        return '/dashboard/owner/overview';
      default:
        return '/';
    }
  }

  setUser(user: AuthUser, accessToken: string): void {
    this.currentUser.set(user);
    this.isAuthenticated.set(true);
    this._accessToken.set(accessToken);
    localStorage.setItem('auth_user', JSON.stringify(user));
    this.scheduleAutoLogout(accessToken);
  }

  logout(): void {
    this.http
      .post(`${environment.apiUrl}${API_ENDPOINTS.AUTH.LOGOUT}`, {}, { withCredentials: true })
      .subscribe({ error: () => {} });
    this.clearAuth();
  }

  private clearAuth(): void {
    if (this.logoutTimer) {
      clearTimeout(this.logoutTimer);
      this.logoutTimer = null;
    }
    this.currentUser.set(null);
    this.isAuthenticated.set(false);
    this._accessToken.set(null);
    localStorage.removeItem('auth_user');
  }

  getToken(): string | null {
    return this._accessToken();
  }

  hasRole(role: UserRole): boolean {
    return this.userRole() === role;
  }

  hasAnyRole(...roles: UserRole[]): boolean {
    const currentRole = this.userRole();
    return currentRole ? roles.includes(currentRole) : false;
  }
}