import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export type UserRole =
  | 'admin'
  | 'company'
  | 'broker'
  | 'tenant'
  | 'maintenance-company';

export interface AuthUser {
  id: string;
  email: string;
  fullName: string;
  role: UserRole;
  companyId: string | null;
  isEmailVerified: boolean;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: AuthUser;
}

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly http = inject(HttpClient);

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

  login(email: string, password: string) {
    return this.http.post<LoginResponse>(
      `${environment.apiUrl}/auth/login`,
      { email, password }
    );
  }

  setUser(user: AuthUser, accessToken: string, refreshToken?: string): void {
    this.currentUser.set(user);
    this.isAuthenticated.set(true);
    localStorage.setItem('auth_user', JSON.stringify(user));
    localStorage.setItem('auth_token', accessToken);
    if (refreshToken) {
      localStorage.setItem('auth_refresh_token', refreshToken);
    }
  }

  logout(): void {
    this.clearAuth();
  }

  private clearAuth(): void {
    this.currentUser.set(null);
    this.isAuthenticated.set(false);
    localStorage.removeItem('auth_user');
    localStorage.removeItem('auth_token');
    localStorage.removeItem('auth_refresh_token');
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
