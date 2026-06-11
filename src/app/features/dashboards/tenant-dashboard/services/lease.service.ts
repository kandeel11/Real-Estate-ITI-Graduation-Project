import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, catchError } from 'rxjs';
import { environment } from '../../../../../environments/environment';

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

export interface RentedProperty {
  id: string;
  title: string;
  address: string;
  type: string;
  bedrooms: number;
  bathrooms: number;
  area: number;
  mainImage: string;
  monthlyRent: number;
  leaseStatus: 'Active' | 'Past';
  leaseStart: string;
  leaseEnd: string;
  leaseId: string;
}

export interface PropertyMedia {
  id: string;
  url: string;
  type: 'image' | 'video';
}

export interface Amenity {
  id: string;
  name: string;
  icon: string;
}

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

const MOCK_RENTED_PROPERTIES: RentedProperty[] = [
  {
    id: 'prop-001',
    title: 'Oak Street Modern Suite',
    address: '42 Oak Street, Suite 3B, New York, NY 10001',
    type: 'Apartment',
    bedrooms: 2,
    bathrooms: 2,
    area: 950,
    mainImage: 'https://placehold.co/800x400/dce9ff/4f46e5?text=Oak+Street+Suite',
    monthlyRent: 2800,
    leaseStatus: 'Active',
    leaseStart: '2024-03-01',
    leaseEnd: '2025-09-30',
    leaseId: 'lease-001',
  },
];

const MOCK_PROPERTY_MEDIA: PropertyMedia[] = [
  { id: 'm-001', url: 'https://placehold.co/400x300/e5eeff/4f46e5?text=Living+Room', type: 'image' },
  { id: 'm-002', url: 'https://placehold.co/400x300/e5eeff/4f46e5?text=Kitchen', type: 'image' },
  { id: 'm-003', url: 'https://placehold.co/400x300/e5eeff/4f46e5?text=Bedroom', type: 'image' },
  { id: 'm-004', url: 'https://placehold.co/400x300/e5eeff/4f46e5?text=Bathroom', type: 'image' },
];

const MOCK_AMENITIES: Amenity[] = [
  { id: 'a-001', name: 'Central Air', icon: 'fa-solid fa-wind' },
  { id: 'a-002', name: 'In-unit Laundry', icon: 'fa-solid fa-shirt' },
  { id: 'a-003', name: 'Gym', icon: 'fa-solid fa-dumbbell' },
  { id: 'a-004', name: 'Parking', icon: 'fa-solid fa-car' },
  { id: 'a-005', name: 'Elevator', icon: 'fa-solid fa-elevator' },
  { id: 'a-006', name: 'Doorman', icon: 'fa-solid fa-user-tie' },
];

@Injectable({ providedIn: 'root' })
export class LeaseService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  getActiveLease(): Observable<Lease | null> {
    // TODO: replace with real API call
    return of(MOCK_ACTIVE_LEASE);
    // return this.http.get<Lease>(`${this.base}/tenant/leases/active`).pipe(catchError(() => of(null)));
  }

  getLeaseHistory(): Observable<Lease[]> {
    // TODO: replace with real API call
    return of(MOCK_LEASE_HISTORY);
    // return this.http.get<Lease[]>(`${this.base}/tenant/leases`).pipe(catchError(() => of([])));
  }

  downloadContract(leaseId: string): Observable<LeaseDownload> {
    return this.http.get<LeaseDownload>(`${this.base}/leases/${leaseId}/download`);
  }

  getRentedProperties(): Observable<RentedProperty[]> {
    // TODO: replace with real API call
    return of(MOCK_RENTED_PROPERTIES);
    // return this.http.get<RentedProperty[]>(`${this.base}/tenant/rented-properties`).pipe(catchError(() => of([])));
  }

  getPropertyMedia(propertyId: string): Observable<PropertyMedia[]> {
    // TODO: replace with real API call
    return of(MOCK_PROPERTY_MEDIA);
    // return this.http.get<PropertyMedia[]>(`${this.base}/properties/${propertyId}/media`).pipe(catchError(() => of([])));
  }

  getPropertyAmenities(propertyId: string): Observable<Amenity[]> {
    // TODO: replace with real API call
    return of(MOCK_AMENITIES);
    // return this.http.get<Amenity[]>(`${this.base}/amenities/property/${propertyId}`).pipe(catchError(() => of([])));
  }
}
