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

/**
 * Backend PropertyType enum index (CLAUDE.md): 0 Apartment, 1 Villa, 2 House.
 * Needed for the JSON PUT body — the API has no string-enum converter, so the
 * update endpoint expects the numeric value (create uses multipart, which binds
 * the string fine).
 */
const PROPERTY_TYPE_INDEX: Record<string, number> = {
  Apartment: 0,
  Villa: 1,
  House: 2,
};

export function propertyTypeToIndex(type: string): number {
  return PROPERTY_TYPE_INDEX[type] ?? 0;
}

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

/** Matches the backend's PropertyMediaDto. */
export interface PropertyMedia {
  id: string;
  mediaUrl: string;
  mediaType: string;
  isPrimary: boolean;
  sortOrder: number;
  fileSizeKb: number;
  uploadedAt: string;
}

/** Matches the backend's PropertyDetailDto (GET /api/properties/{id}). */
export interface PropertyDetail {
  id: string;
  ownerId: string;
  companyId: string | null;
  listingBrokerId: string | null;
  title: string;
  titleAr: string;
  description: string;
  descriptionAr: string;
  propertyType: string;
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
  city: string;
  district: string;
  country: string;
  governorate: string;
  listingSource: string;
  status: PropertyStatus;
  isFeatured: boolean;
  viewsCount: number;
  inquiriesCount: number;
  createdAt: string;
  media: PropertyMedia[];
  amenities: Amenity[];
  // Enriched by the backend's PropertyEnrichmentService
  listingDisplayName: string | null;
  listingBrokerName: string | null;
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
  amenityIds?: string[]; // present on /properties/active; absent in offline mocks
  // Enriched by the backend's PropertyEnrichmentService
  listingDisplayName: string | null;
  listingBrokerName: string | null;
}
