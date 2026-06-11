/**
 * Property Service
 * The Property module IS implemented on the backend.
 * Company dashboard uses GET /api/companies/mine/properties (CompanyManager JWT).
 * `useMock` is an offline fallback for demos when the backend isn't running
 * or no company-manager token is available yet.
 */

import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, delay } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { API_ENDPOINTS } from '../../../core/constants/api-endpoints';
import { Property } from '../models/property.model';

const MOCK_PROPERTIES: Property[] = [
  { id: 'p1', ownerId: 'u1', companyId: 'c1', listingBrokerId: 'b1', title: 'Maadi 3BR Apartment', titleAr: '',
    propertyType: 'Apartment', price: 8000, bedrooms: 3, bathrooms: 2, areaSqm: 140, isFurnished: true,
    petsAllowed: false, city: 'Cairo', district: 'Maadi', status: 'Active', isFeatured: true,
    primaryImageUrl: null, viewsCount: 245, createdAt: '2026-05-01T10:00:00Z',
    listingDisplayName: 'Acme Real Estate', listingBrokerName: 'Sara Hassan' },
  { id: 'p2', ownerId: 'u1', companyId: 'c1', listingBrokerId: 'b2', title: '6 October Villa', titleAr: '',
    propertyType: 'Villa', price: 25000, bedrooms: 5, bathrooms: 4, areaSqm: 380, isFurnished: false,
    petsAllowed: true, city: 'Giza', district: '6 October', status: 'PendingReview', isFeatured: false,
    primaryImageUrl: null, viewsCount: 0, createdAt: '2026-06-05T09:00:00Z',
    listingDisplayName: 'Acme Real Estate', listingBrokerName: 'Mahmoud Ali' },
  { id: 'p3', ownerId: 'u1', companyId: 'c1', listingBrokerId: 'b1', title: 'Zamalek Studio', titleAr: '',
    propertyType: 'Apartment', price: 6500, bedrooms: 1, bathrooms: 1, areaSqm: 60, isFurnished: true,
    petsAllowed: false, city: 'Cairo', district: 'Zamalek', status: 'Draft', isFeatured: false,
    primaryImageUrl: null, viewsCount: 12, createdAt: '2026-06-08T14:00:00Z',
    listingDisplayName: 'Acme Real Estate', listingBrokerName: 'Sara Hassan' },
  { id: 'p4', ownerId: 'u1', companyId: 'c1', listingBrokerId: 'b3', title: 'New Cairo Townhouse', titleAr: '',
    propertyType: 'House', price: 18000, bedrooms: 4, bathrooms: 3, areaSqm: 250, isFurnished: false,
    petsAllowed: true, city: 'Cairo', district: 'Fifth Settlement', status: 'Rented', isFeatured: false,
    primaryImageUrl: null, viewsCount: 510, createdAt: '2026-04-12T11:00:00Z',
    listingDisplayName: 'Acme Real Estate', listingBrokerName: 'Layla Ibrahim' },
];

@Injectable({ providedIn: 'root' })
export class PropertyService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;
  private useMock = false; // real endpoint is live — flip only for offline demos

  /** Properties of the logged-in manager's company (CompanyId from JWT). */
  getCompanyProperties(): Observable<Property[]> {
    if (this.useMock) return of([...MOCK_PROPERTIES]).pipe(delay(300));
    return this.http.get<Property[]>(
      `${this.baseUrl}${API_ENDPOINTS.COMPANIES.MINE_PROPERTIES}`
    );
  }

  /** Public active listings. */
  getActive(): Observable<Property[]> {
    if (this.useMock) {
      return of(MOCK_PROPERTIES.filter((p) => p.status === 'Active')).pipe(delay(300));
    }
    return this.http.get<Property[]>(
      `${this.baseUrl}${API_ENDPOINTS.PROPERTIES.GET_ACTIVE}`
    );
  }

  getById(id: string): Observable<Property> {
    if (this.useMock) {
      return of(MOCK_PROPERTIES.find((p) => p.id === id)!).pipe(delay(200));
    }
    return this.http.get<Property>(
      `${this.baseUrl}${API_ENDPOINTS.PROPERTIES.GET_BY_ID(id)}`
    );
  }
}
