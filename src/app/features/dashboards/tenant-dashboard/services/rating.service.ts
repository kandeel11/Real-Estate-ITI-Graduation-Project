import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, catchError } from 'rxjs';
import { environment } from '../../../../../environments/environment';

export type RatingStatus = 'Pending' | 'Approved' | 'Rejected';
export type RatingTargetType = 'Property' | 'MaintenanceCompany';

export interface Rating {
  id: string;
  rating: number;
  reviewText: string;
  targetType: RatingTargetType;
  targetName: string;
  status: RatingStatus;
  createdAt: string;
  propertyId?: string;
  maintenanceCompanyId?: string;
}

export interface CreateRatingDto {
  rating: number;
  reviewText: string;
  targetType: RatingTargetType;
  propertyId?: string;
  maintenanceCompanyId?: string;
}

const MOCK_RATINGS: Rating[] = [
  {
    id: 'r-001', rating: 4,
    reviewText: 'Great property! The apartment is spacious and well-maintained. The location is perfect. Minor issues with water pressure but overall very satisfied.',
    targetType: 'Property', targetName: '42 Oak Street, Suite 3B',
    status: 'Approved',
    createdAt: new Date(Date.now() - 2592000000).toISOString(),
    propertyId: 'prop-001',
  },
  {
    id: 'r-002', rating: 5,
    reviewText: 'QuickFix services were excellent! The technician arrived on time, was professional, and resolved the issue quickly. Highly recommend.',
    targetType: 'MaintenanceCompany', targetName: 'QuickFix Maintenance Services',
    status: 'Pending',
    createdAt: new Date(Date.now() - 864000000).toISOString(),
    maintenanceCompanyId: 'mc-001',
  },
];

@Injectable({ providedIn: 'root' })
export class RatingService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  getMyRatings(): Observable<Rating[]> {
    // TODO: replace with real API call
    return of(MOCK_RATINGS);
    // return this.http.get<Rating[]>(`${this.base}/ratings/mine`).pipe(catchError(() => of([])));
  }

  createRating(data: CreateRatingDto): Observable<Rating> {
    return this.http.post<Rating>(`${this.base}/ratings`, data);
  }

  updateRating(id: string, data: Partial<CreateRatingDto>): Observable<Rating> {
    return this.http.put<Rating>(`${this.base}/ratings/${id}`, data);
  }

  deleteRating(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/ratings/${id}`);
  }
}
