import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { timeout } from 'rxjs/operators';
import { environment } from '../../../../../environments/environment';

export interface MaintenanceCategory {
  id: string;
  name: string;
  icon: string;
  description: string;
  isActive: boolean;
}

export interface MaintenanceCompanyProfile {
  id: string;
  companyName: string;
  email: string;
  phone: string;
  city: string;
  serviceRadiusKm: number;
  logoUrl: string | null;
  description: string;
  isActive: boolean;
  averageRating: number;
  completedTicketsCount: number;
  activeTicketsCount: number;
  createdAt: string;
  categories: MaintenanceCategory[];
}

export interface UpdateProfileRequest {
  companyName: string;
  phone: string;
  city: string;
  serviceRadiusKm: number;
  description: string;
  logoUrl: string | null;
  categoryIds: string[];
}

@Injectable({ providedIn: 'root' })
export class MaintenanceProfileService {
  private readonly http = inject(HttpClient);

  getProfile(): Observable<MaintenanceCompanyProfile> {
    return this.http
      .get<MaintenanceCompanyProfile>(`${environment.apiUrl}/maintenance-companies/profile`)
      .pipe(timeout(15000));
  }

  updateProfile(body: UpdateProfileRequest): Observable<MaintenanceCompanyProfile> {
    return this.http
      .put<MaintenanceCompanyProfile>(`${environment.apiUrl}/maintenance-companies/profile`, body)
      .pipe(timeout(15000));
  }
}
