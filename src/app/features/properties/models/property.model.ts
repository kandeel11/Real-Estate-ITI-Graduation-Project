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
