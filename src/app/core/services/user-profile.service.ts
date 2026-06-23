/**
 * User Profile Service
 * Role-agnostic self-service profile management against /api/users/me —
 * used by the admin settings page (tenants have their own /tenant/profile).
 */

import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface UserProfile {
  id: string;
  email: string;
  fullName: string;
  phone: string;
  role: string;
  companyId: string | null;
  isActive: boolean;
  isEmailVerified: boolean;
}

export interface UpdateMyProfileRequest {
  fullName: string;
  phone: string;
  profileImageUrl?: string | null;
}

export interface ChangeMyPasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
}

@Injectable({ providedIn: 'root' })
export class UserProfileService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  getMe(): Observable<UserProfile> {
    return this.http.get<UserProfile>(`${this.baseUrl}/users/me`);
  }

  updateProfile(req: UpdateMyProfileRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/users/me/profile`, req);
  }

  changePassword(req: ChangeMyPasswordRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/users/me/password`, req);
  }
}
