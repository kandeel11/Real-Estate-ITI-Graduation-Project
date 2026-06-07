/**
 * Authentication Service
 * Manages user authentication state via Angular Signals
 * Provides reactive role-based access control
 */

import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export interface AuthUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: UserRole;
  profileImage?: string;
}

export type UserRole =
  | 'admin'
  | 'company'
  | 'broker'
  | 'tenant'
  | 'maintenance-company';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly http = inject(HttpClient);

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
   * Authenticate user with credentials
   */
  login(email: string, password: string) {
    this.isLoading.set(true);
    return this.http
      .post<{ user: AuthUser; token: string }>(
        `${environment.apiUrl}/auth/login`,
        { email, password }
      );
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
