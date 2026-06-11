import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, catchError } from 'rxjs';
import { environment } from '../../../../../environments/environment';

export interface UserProfile {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  role: string;
  profileImage?: string;
}

export interface UpdateProfileDto {
  firstName: string;
  lastName: string;
  phone: string;
}

export interface ChangePasswordDto {
  currentPassword: string;
  newPassword: string;
}

const MOCK_PROFILE: UserProfile = {
  id: 'u-001',
  firstName: 'Sarah',
  lastName: 'Johnson',
  email: 'sarah.johnson@example.com',
  phone: '+1 (212) 555-0142',
  role: 'tenant',
};

@Injectable({ providedIn: 'root' })
export class ProfileService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  getProfile(): Observable<UserProfile> {
    // TODO: replace with real API call
    return of(MOCK_PROFILE);
    // return this.http.get<UserProfile>(`${this.base}/auth/me`).pipe(catchError(() => of(MOCK_PROFILE)));
  }

  updateProfile(data: UpdateProfileDto): Observable<UserProfile> {
    return this.http.put<UserProfile>(`${this.base}/tenant/profile`, data);
  }

  changePassword(data: ChangePasswordDto): Observable<void> {
    return this.http.put<void>(`${this.base}/tenant/profile/password`, data);
  }
}
