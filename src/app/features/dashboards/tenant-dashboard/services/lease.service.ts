import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../../../../environments/environment';

// ── API lease shape ─────────────────────────────────────────────────────────

export interface ApiLease {
  id: string;
  propertyId: string;
  tenantId: string;
  ownerId: string;
  rentAmount: number;
  securityDeposit: number;
  startDate: string;
  endDate: string;
  paymentDayOfMonth: number;
  extractionStatus: string;
  extractionConfidence: number;
  isActive: boolean;
  contractFileUrl: string;
}

// ── Rented-properties API types ──────────────────────────────────────────────

export interface ApiPropertyMedia {
  id: string;
  mediaUrl: string;
  mediaType: string;
  isPrimary?: boolean;
}

export interface ApiAmenity {
  id: string;
  name: string;
  nameAr?: string;
  icon: string;
}

export interface ApiPropertyDto {
  id: string;
  title: string;
  propertyType: string;
  price: number;
  bedrooms: number;
  bathrooms: number;
  areaSqm: number;
  address: string;
  apartmentNumber: number | null;
  city: string;
  district: string;
  country: string;
  governorate: string;
  isFurnished: boolean;
  petsAllowed: boolean;
  status: string;
  media: ApiPropertyMedia[];
  amenities: ApiAmenity[];
}

export interface LeaseOffer {
  id: string;
  propertyId: string;
  propertyTitle: string;
  startDate: string;
  endDate: string;
  monthlyRent: number;
  deposit: number;
  pdfUrl?: string;
  status: 'pending_tenant' | 'accepted' | 'declined';
  sentAt: string;
}

export interface RentedPropertyItem {
  property: ApiPropertyDto;
  leaseId: string;
  monthlyRent: number;
  startDate: string;
  endDate: string;
  isActive: boolean;
  daysUntilExpiry: number;
}

@Injectable({ providedIn: 'root' })
export class LeaseService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  getLeases(): Observable<ApiLease[]> {
    return this.http.get<ApiLease[]>(`${this.base}/tenant/leases`);
  }

  getActiveLease(): Observable<ApiLease> {
    return this.http.get<ApiLease>(`${this.base}/tenant/leases/active`);
  }

  getRentedProperties(): Observable<RentedPropertyItem[]> {
    return this.http.get<RentedPropertyItem[]>(`${this.base}/tenant/rented-properties`);
  }

  getPendingOffer(): Observable<LeaseOffer | null> {
    return this.http.get<LeaseOffer>(`${this.base}/tenant/leases/pending-offer`)
      .pipe(catchError(() => of(null)));
  }

  acceptOffer(offerId: string): Observable<void> {
    return this.http.post<void>(`${this.base}/tenant/leases/${offerId}/accept`, {});
  }

  declineOffer(offerId: string): Observable<void> {
    return this.http.post<void>(`${this.base}/tenant/leases/${offerId}/decline`, {});
  }

  contractUrl(relativePath: string): string {
    return `${this.base.replace('/api', '')}/${relativePath}`;
  }
}
