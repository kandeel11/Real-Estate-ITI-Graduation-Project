/**
 * Property Service
 * The Property module IS implemented on the backend.
 * Company dashboard uses GET /api/companies/mine/properties (CompanyManager JWT).
 * `useMock` is an offline fallback for demos when the backend isn't running
 * or no company-manager token is available yet.
 */

import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, delay, map } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { API_ENDPOINTS } from '../../../core/constants/api-endpoints';
import {
  Property,
  PropertyDetail,
  Amenity,
  DescriptionDraft,
  CreatePropertyData,
  propertyTypeToIndex,
} from '../models/property.model';

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

  /**
   * Tenant submits an inquiry on a listing. TenantId comes from the JWT;
   * the property's owner/company are read from its detail.
   */
  createInquiry(propertyId: string, ownerId: string, companyId: string | null, message: string): Observable<unknown> {
    return this.http.post(`${this.baseUrl}${API_ENDPOINTS.INQUIRIES.CREATE}`, {
      propertyId,
      ownerId,
      companyId,
      message,
    });
  }

  /**
   * Whether the tenant may send a new inquiry right now (one engagement at a time),
   * and a human-readable reason when they can't.
   */
  canCreateInquiry(): Observable<{ allowed: boolean; reason: string; reasonCode: string }> {
    return this.http.get<{ allowed: boolean; reason: string; reasonCode: string }>(
      `${this.baseUrl}${API_ENDPOINTS.INQUIRIES.CAN_CREATE}`,
    );
  }

  getById(id: string): Observable<PropertyDetail> {
    return this.http.get<PropertyDetail>(
      `${this.baseUrl}${API_ENDPOINTS.PROPERTIES.GET_BY_ID(id)}`
    );
  }

  /** GET /api/properties/mine — the caller's own listings, all statuses. */
  getMine(): Observable<Property[]> {
    return this.http.get<Property[]>(
      `${this.baseUrl}${API_ENDPOINTS.PROPERTIES.GET_MINE}`
    );
  }

  /** DELETE /api/properties/{id} — soft delete; frees the plan slot. */
  delete(propertyId: string): Observable<void> {
    return this.http.delete<void>(
      `${this.baseUrl}${API_ENDPOINTS.PROPERTIES.DELETE(propertyId)}`
    );
  }

  /** POST /api/properties/{id}/mark-rented */
  markAsRented(propertyId: string): Observable<void> {
    return this.http.post<void>(
      `${this.baseUrl}/properties/${propertyId}/mark-rented`,
      {}
    );
  }

  /** POST /api/properties/{id}/feature — feature or unfeature a listing (premium only, 1 at a time). */
  setFeatured(propertyId: string, isFeatured: boolean): Observable<void> {
    return this.http.post<void>(
      `${this.baseUrl}${API_ENDPOINTS.PROPERTIES.SET_FEATURED(propertyId)}`,
      { isFeatured, featuredUntil: null }
    );
  }

  // ─── Creation flow ────────────────────────────────────────────────
  // POST /api/properties is multipart/form-data; keys match the backend's
  // CreatePropertyRequest. Ownership files are required only for owners —
  // for brokers the backend skips the ownership verification entirely.

  create(data: CreatePropertyData): Observable<{ id: string }> {
    const form = new FormData();
    form.append('Title', data.title);
    form.append('TitleAr', data.titleAr);
    form.append('PropertyType', data.propertyType);
    form.append('Price', String(data.price));
    form.append('Bedrooms', String(data.bedrooms));
    form.append('Bathrooms', String(data.bathrooms));
    form.append('AreaSqm', String(data.areaSqm));
    form.append('FloorNumber', String(data.floorNumber));
    form.append('TotalFloors', String(data.totalFloors));
    form.append('YearBuilt', String(data.yearBuilt));
    form.append('IsFurnished', String(data.isFurnished));
    form.append('PetsAllowed', String(data.petsAllowed));
    form.append('Address', data.address);
    if (data.apartmentNumber != null)
      form.append('ApartmentNumber', String(data.apartmentNumber));
    form.append('Country', data.country);
    form.append('Governorate', data.governorate);
    form.append('City', data.city);
    form.append('District', data.district);
    form.append('ListingSource', data.listingSource);
    if (data.ownerFullName) form.append('OwnerFullName', data.ownerFullName);
    if (data.nationalIdFile)
      form.append('NationalIdFile', data.nationalIdFile);
    if (data.ownershipProofFile)
      form.append('OwnershipProofFile', data.ownershipProofFile);

    return this.http.post<{ id: string }>(
      `${this.baseUrl}${API_ENDPOINTS.PROPERTIES.CREATE}`,
      form
    );
  }

  setPrimaryMedia(propertyId: string, mediaId: string): Observable<void> {
    return this.http.put<void>(
      `${this.baseUrl}${API_ENDPOINTS.PROPERTIES.SET_PRIMARY_MEDIA(propertyId, mediaId)}`,
      {}
    );
  }

  deleteMedia(propertyId: string, mediaId: string): Observable<void> {
    return this.http.delete<void>(
      `${this.baseUrl}${API_ENDPOINTS.PROPERTIES.DELETE_MEDIA(propertyId, mediaId)}`
    );
  }

  uploadMedia(propertyId: string, file: File, sortOrder = 0): Observable<{ mediaId: string }> {
    const form = new FormData();
    form.append('File', file);
    form.append('MediaType', 'Image');
    form.append('SortOrder', String(sortOrder));
    return this.http.post<{ mediaId: string }>(
      `${this.baseUrl}${API_ENDPOINTS.PROPERTIES.MEDIA(propertyId)}`,
      form
    );
  }

  getAmenities(): Observable<Amenity[]> {
    return this.http
      .get<Amenity[]>(`${this.baseUrl}${API_ENDPOINTS.AMENITIES.GET_ALL}`)
      // "Pet Friendly" is dropped — it duplicates the PetsAllowed property field.
      .pipe(map((list) => list.filter((a) => a.name !== 'Pet Friendly')));
  }

  addAmenity(propertyId: string, amenityId: string): Observable<void> {
    return this.http.post<void>(
      `${this.baseUrl}${API_ENDPOINTS.PROPERTIES.AMENITIES(propertyId)}`,
      { amenityId }
    );
  }

  removeAmenity(propertyId: string, amenityId: string): Observable<void> {
    return this.http.delete<void>(
      `${this.baseUrl}${API_ENDPOINTS.PROPERTIES.REMOVE_AMENITY(propertyId, amenityId)}`
    );
  }

  requestAiDescription(propertyId: string): Observable<unknown> {
    return this.http.post(
      `${this.baseUrl}${API_ENDPOINTS.PROPERTIES.REQUEST_AI_DESCRIPTION(propertyId)}`,
      {}
    );
  }

  /** 404 until the AI handler has produced a draft — callers poll this. */
  getLatestDescription(propertyId: string): Observable<DescriptionDraft> {
    return this.http.get<DescriptionDraft>(
      `${this.baseUrl}${API_ENDPOINTS.AI.DESCRIPTION_BY_PROPERTY(propertyId)}`
    );
  }

  acceptDescription(generationId: string, finalTextEn: string, finalTextAr: string): Observable<unknown> {
    return this.http.patch(
      `${this.baseUrl}${API_ENDPOINTS.AI.ACCEPT_DESCRIPTION(generationId)}`,
      { finalTextEn, finalTextAr }
    );
  }

  /**
   * PUT /api/properties/{id} — full update; used by the wizard to save a
   * manually written description (the body must carry all fields, so the
   * step-1 values are passed back through).
   */
  update(
    propertyId: string,
    data: CreatePropertyData,
    description: string,
    descriptionAr: string,
    listingBrokerId: string | null
  ): Observable<void> {
    return this.http.put<void>(
      `${this.baseUrl}${API_ENDPOINTS.PROPERTIES.UPDATE(propertyId)}`,
      {
        title: data.title,
        titleAr: data.titleAr,
        description,
        descriptionAr,
        // numeric — the JSON PUT has no string-enum converter on the backend
        propertyType: propertyTypeToIndex(data.propertyType),
        price: data.price,
        bedrooms: data.bedrooms,
        bathrooms: data.bathrooms,
        areaSqm: data.areaSqm,
        floorNumber: data.floorNumber,
        totalFloors: data.totalFloors,
        yearBuilt: data.yearBuilt,
        isFurnished: data.isFurnished,
        petsAllowed: data.petsAllowed,
        address: data.address,
        apartmentNumber: data.apartmentNumber,
        country: data.country,
        governorate: data.governorate,
        city: data.city,
        district: data.district,
        listingBrokerId,
      }
    );
  }

  /** PATCH /api/properties/{id}/assign-broker — company manager only. */
  assignBroker(propertyId: string, brokerId: string | null): Observable<void> {
    return this.http.patch<void>(
      `${this.baseUrl}${API_ENDPOINTS.PROPERTIES.ASSIGN_BROKER(propertyId)}`,
      { brokerId }
    );
  }

  submitForReview(propertyId: string): Observable<void> {
    return this.http.post<void>(
      `${this.baseUrl}${API_ENDPOINTS.PROPERTIES.SUBMIT(propertyId)}`,
      {}
    );
  }
}
