import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../../environments/environment';

export type ModerationStatus = 'Pending' | 'Approved' | 'Rejected';

export interface RatingResponse {
  id: string;
  reviewerId: string;
  leaseId: string | null;
  propertyId: string | null;
  maintenanceCompanyId: string | null;
  ratingValue: number;
  reviewText: string | null;
  moderationStatus: ModerationStatus;
  createdAt: string;
  updatedAt: string;
}

export interface CreateRatingRequest {
  leaseId: string | null;
  propertyId: string | null;
  maintenanceCompanyId: string | null;
  ratingValue: number;
  reviewText: string | null;
}

export interface UpdateRatingRequest {
  ratingValue: number;
  reviewText: string | null;
}

export interface PropertyRatingsResult {
  averageRating: number;
  totalCount: number;
  items: RatingResponse[];
}

@Injectable({ providedIn: 'root' })
export class RatingService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  getMyRatings(): Observable<RatingResponse[]> {
    return this.http.get<RatingResponse[]>(`${this.base}/ratings/mine`);
  }

  createRating(data: CreateRatingRequest): Observable<RatingResponse> {
    return this.http.post<RatingResponse>(`${this.base}/ratings`, data);
  }

  updateRating(id: string, data: UpdateRatingRequest): Observable<RatingResponse> {
    return this.http.put<RatingResponse>(`${this.base}/ratings/${id}`, data);
  }

  deleteRating(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/ratings/${id}`);
  }

  getPropertyRatings(propertyId: string): Observable<PropertyRatingsResult> {
    return this.http.get<PropertyRatingsResult>(`${this.base}/ratings/property/${propertyId}`);
  }
}
