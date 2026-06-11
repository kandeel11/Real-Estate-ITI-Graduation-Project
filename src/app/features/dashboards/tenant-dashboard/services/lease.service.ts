import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { environment } from '../../../../../environments/environment';

// ── Lease interfaces (unchanged — used by My Lease page) ────────────────────

export interface Lease {
  id: string;
  propertyId: string;
  propertyAddress: string;
  monthlyRent: number;
  securityDeposit: number;
  startDate: string;
  endDate: string;
  paymentDayOfMonth: number;
  status: 'Active' | 'Ended';
  daysUntilExpiry: number;
}

export interface LeaseDownload {
  downloadUrl: string;
}

// ── Rented-properties API types ──────────────────────────────────────────────

export interface ApiPropertyMedia {
  id: string;
  url: string;
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

export interface RentedPropertyItem {
  property: ApiPropertyDto;
  leaseId: string;
  monthlyRent: number;
  startDate: string;
  endDate: string;
  isActive: boolean;
  daysUntilExpiry: number;
}

// ── Mock data for My Lease (still mock) ─────────────────────────────────────

const MOCK_ACTIVE_LEASE: Lease = {
  id: 'lease-001',
  propertyId: 'prop-001',
  propertyAddress: '42 Oak Street, Suite 3B, New York, NY 10001',
  monthlyRent: 2800,
  securityDeposit: 5600,
  startDate: '2024-03-01',
  endDate: '2025-09-30',
  paymentDayOfMonth: 1,
  status: 'Active',
  daysUntilExpiry: 45,
};

const MOCK_LEASE_HISTORY: Lease[] = [
  MOCK_ACTIVE_LEASE,
  {
    id: 'lease-000',
    propertyId: 'prop-002',
    propertyAddress: '15 Maple Avenue, Apt 2F, Brooklyn, NY 11201',
    monthlyRent: 2200,
    securityDeposit: 4400,
    startDate: '2022-06-01',
    endDate: '2023-05-31',
    paymentDayOfMonth: 1,
    status: 'Ended',
    daysUntilExpiry: 0,
  },
];

@Injectable({ providedIn: 'root' })
export class LeaseService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  getRentedProperties(): Observable<RentedPropertyItem[]> {
    return this.http.get<RentedPropertyItem[]>(`${this.base}/tenant/rented-properties`);
  }

  getActiveLease(): Observable<Lease | null> {
    // TODO: replace with real API call
    return of(MOCK_ACTIVE_LEASE);
  }

  getLeaseHistory(): Observable<Lease[]> {
    // TODO: replace with real API call
    return of(MOCK_LEASE_HISTORY);
  }

  downloadContract(leaseId: string): Observable<LeaseDownload> {
    return this.http.get<LeaseDownload>(`${this.base}/leases/${leaseId}/download`);
  }
}
