import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../../environments/environment';

export interface UserProfile {
  id: string;
  fullName: string;
  email: string;
  phone: string;
  role: string;
  isEmailVerified: boolean;
  isActive: boolean;
}

export interface UpdateProfileDto {
  fullName: string;
  phone: string;
  email: string;
}

export interface ChangePasswordDto {
  currentPassword: string;
  newPassword: string;
}

@Injectable({ providedIn: 'root' })
export class ProfileService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  getProfile(): Observable<UserProfile> {
    return this.http.get<UserProfile>(`${this.base}/tenant/profile`);
  }

  updateProfile(data: UpdateProfileDto): Observable<UserProfile> {
    return this.http.put<UserProfile>(`${this.base}/tenant/profile`, data);
  }

  changePassword(data: ChangePasswordDto): Observable<void> {
    return this.http.put<void>(`${this.base}/tenant/profile/password`, data);
  }

  resendVerification(email: string): Observable<void> {
    return this.http.post<void>(`${this.base}/auth/resend-verification`, { email });
  }
}
