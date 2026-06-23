import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError, timeout } from 'rxjs/operators';
import { environment } from '../../../../../environments/environment';

// ── API lease shape ─────────────────────────────────────────────────────────

/** Lifecycle status derived by the backend (LeaseDto.DeriveStatus). */
export type LeaseLifecycleStatus =
  | 'pending'
  | 'active'
  | 'renewal_requested'
  | 'termination_requested'
  | 'terminated'
  | 'declined'
  | 'ended';

export interface ApiLease {
  id: string;
  propertyId: string;
  tenantId: string;
  ownerId: string;
  installmentAmount: number;
  totalRentAmount?: number | null;
  paymentFrequency?: string | null;
  durationInMonths?: number | null;
  securityDeposit?: number | null;
  startDate: string;
  endDate: string;
  paymentDayOfMonth: number;
  extractionStatus: string;
  extractionConfidence?: number | null;
  isActive: boolean;
  contractFileUrl: string;
  createdAt?: string;
  // ── Renewal / termination lifecycle ───────────────────────────────────────
  renewedFromLeaseId?: string | null;
  renewalRequestedAt?: string | null;
  terminationRequestedById?: string | null;
  terminationRequestedAt?: string | null;
  terminationEffectiveDate?: string | null;
  status: LeaseLifecycleStatus;
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
  /** Value of ONE installment (matches the backend's `installmentAmount`). */
  installmentAmount: number;
  paymentFrequency?: string | null;
  totalRentAmount?: number | null;
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
    // Accept runs lease activation + downstream event handlers synchronously, so it
    // can take several seconds. Cap it so a hung request surfaces an error (and lets
    // the tenant retry) instead of spinning forever.
    return this.http
      .post<void>(`${this.base}/tenant/leases/${offerId}/accept`, {})
      .pipe(timeout(30000));
  }

  declineOffer(offerId: string): Observable<void> {
    return this.http
      .post<void>(`${this.base}/tenant/leases/${offerId}/decline`, {})
      .pipe(timeout(30000));
  }

  getContractUrl(leaseId: string): Observable<{ contractFileUrl: string }> {
    return this.http.get<{ contractFileUrl: string }>(`${this.base}/leases/${leaseId}/download`);
  }

  // ── Renewal / termination (tenant side) ─────────────────────────────────────

  /** Tenant asks the landlord to renew an active lease nearing its end. */
  requestRenewal(leaseId: string): Observable<void> {
    return this.http.post<void>(`${this.base}/tenant/leases/${leaseId}/renew-request`, {});
  }

  /** Either party proposes early termination; the other must approve. */
  requestTermination(leaseId: string, reason: string, effectiveDate?: string): Observable<void> {
    return this.http.post<void>(`${this.base}/leases/${leaseId}/terminate-request`, {
      reason,
      effectiveDate: effectiveDate ?? null,
    });
  }

  /** Approve or reject a pending termination request raised by the counterparty. */
  respondTermination(leaseId: string, approve: boolean, note?: string): Observable<void> {
    const action = approve ? 'terminate-approve' : 'terminate-reject';
    return this.http.post<void>(`${this.base}/leases/${leaseId}/${action}`, { note: note ?? null });
  }

  contractUrl(relativePath: string): string {
    return `${this.base.replace('/api', '')}/${relativePath}`;
  }
}
