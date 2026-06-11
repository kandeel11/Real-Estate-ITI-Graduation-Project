/**
 * Property Models
 * Matches the backend's PropertySummaryDto (camelCase JSON).
 * Status and PropertyType are serialized as strings by the backend.
 */

export type PropertyStatus =
  | 'Draft'
  | 'PendingReview'
  | 'Active'
  | 'Rented'
  | 'Rejected'
  | 'RequiresManualReview';

/** Matches the backend's AmenityDto (GET /api/amenities). */
export interface Amenity {
  id: string;
  name: string;
  nameAr: string;
  iconUrl: string | null;
  category: string;
}

/** Matches the backend's DescriptionDraftDto (GET /api/AI/descriptions/by-property/{id}). */
export interface DescriptionDraft {
  generationId: string;
  propertyId: string;
  generatedTextEn: string | null;
  generatedTextAr: string | null;
  isAccepted: boolean;
  createdAt: string;
}

/** Form values for POST /api/properties (sent as multipart/form-data). */
export interface CreatePropertyData {
  title: string;
  titleAr: string;
  propertyType: string; // 'Apartment' | 'Villa' | 'House'
  price: number;
  bedrooms: number;
  bathrooms: number;
  areaSqm: number;
  floorNumber: number;
  totalFloors: number;
  yearBuilt: number;
  isFurnished: boolean;
  petsAllowed: boolean;
  address: string;
  apartmentNumber: number | null;
  country: string;
  governorate: string;
  city: string;
  district: string;
  listingSource: string; // 'Owner' | 'Company'
  // Owner flow only — skipped entirely for brokers
  ownerFullName?: string;
  nationalIdFile?: File | null;
  ownershipProofFile?: File | null;
}

export interface Property {
  id: string;
  ownerId: string;
  companyId: string | null;
  listingBrokerId: string | null;
  title: string;
  titleAr: string;
  propertyType: string;
  price: number;
  bedrooms: number;
  bathrooms: number;
  areaSqm: number;
  isFurnished: boolean;
  petsAllowed: boolean;
  city: string;
  district: string;
  status: PropertyStatus;
  isFeatured: boolean;
  primaryImageUrl: string | null;
  viewsCount: number;
  createdAt: string;
  // Enriched by the backend's PropertyEnrichmentService
  listingDisplayName: string | null;
  listingBrokerName: string | null;
}
