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
import { Observable, of, delay, map, tap, finalize } from 'rxjs';
import { environment } from '../../../environments/environment';
import { API_ENDPOINTS } from '../constants/api-endpoints';

export interface AuthUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: UserRole;
  companyId?: string | null;
  profileImage?: string;
}

export type UserRole =
  | 'admin'
  | 'company'
  | 'broker'
  | 'tenant'
  | 'owner'
  | 'maintenance-company';

interface BackendAuthResponse {
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
  private useMock = false; // ← flip to false to authenticate against the real backend

  // Reactive state using Signals
  private readonly currentUser = signal<AuthUser | null>(null);
  private readonly isAuthenticated = signal<boolean>(false);
  private readonly isLoading = signal<boolean>(false);

  // Computed signals for derived state
  readonly user = this.currentUser.asReadonly();
  readonly authenticated = this.isAuthenticated.asReadonly();
  readonly loading = this.isLoading.asReadonly();
  readonly userRole = computed(() => this.currentUser()?.role || null);

  constructor() {
    this.initializeAuth();
  }

  /**
   * Initialize authentication from stored session
   */
  private initializeAuth(): void {
    const storedUser = localStorage.getItem('auth_user');
    if (storedUser) {
      try {
        const user = JSON.parse(storedUser) as AuthUser;
        this.currentUser.set(user);
        this.isAuthenticated.set(true);
      } catch {
        this.clearAuth();
      }
    }
  }

  /**
   * Authenticate user with credentials.
   * Emits the signed-in user; auth state and tokens are stored as a side effect.
   */
  login(email: string, password: string): Observable<AuthUser> {
    this.isLoading.set(true);

    if (this.useMock) {
      const user = this.mockUserFor(email);
      return of(user).pipe(
        delay(700),
        tap((u) => this.setUser(u, `mock-jwt-${u.role}`))
      );
    }

    return this.http
      .post<BackendAuthResponse>(
        `${environment.apiUrl}${API_ENDPOINTS.AUTH.LOGIN}`,
        { email, password }
      )
      .pipe(
        map((res) => {
          const user = this.toAuthUser(res.user);
          localStorage.setItem('refresh_token', res.refreshToken);
          this.setUser(user, res.accessToken);
          return user;
        }),
        finalize(() => this.isLoading.set(false))
      );
  }

  /** Map the backend's CurrentUserDto to the frontend AuthUser. */
  private toAuthUser(u: BackendAuthResponse['user']): AuthUser {
    const [firstName, ...rest] = (u.fullName ?? '').split(' ');
    return {
      id: u.id,
      email: u.email,
      firstName: firstName || u.email,
      lastName: rest.join(' '),
      role: BACKEND_ROLE_MAP[u.role] ?? 'tenant',
      companyId: u.companyId,
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
      firstName: local.charAt(0).toUpperCase() + local.slice(1),
      lastName: '(Demo)',
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
        return '/dashboard/tenant/overview';
      case 'maintenance-company':
        return '/dashboard/maintenance-company/overview';
      default:
        return '/'; // owner has no dashboard yet
    }
  }

  /**
   * Set authenticated user after successful login
   */
  setUser(user: AuthUser, token: string): void {
    this.currentUser.set(user);
    this.isAuthenticated.set(true);
    localStorage.setItem('auth_user', JSON.stringify(user));
    localStorage.setItem('auth_token', token);
    this.isLoading.set(false);
  }

  /**
   * Clear authentication state
   */
  logout(): void {
    this.clearAuth();
  }

  private clearAuth(): void {
    this.currentUser.set(null);
    this.isAuthenticated.set(false);
    localStorage.removeItem('auth_user');
    localStorage.removeItem('auth_token');
    localStorage.removeItem('refresh_token');
  }

  /**
   * Get stored auth token
   */
  getToken(): string | null {
    return localStorage.getItem('auth_token');
  }

  /**
   * Check if user has specific role
   */
  hasRole(role: UserRole): boolean {
    return this.userRole() === role;
  }

  /**
   * Check if user has any of the provided roles
   */
  hasAnyRole(...roles: UserRole[]): boolean {
    const currentRole = this.userRole();
    return currentRole ? roles.includes(currentRole) : false;
  }
}
