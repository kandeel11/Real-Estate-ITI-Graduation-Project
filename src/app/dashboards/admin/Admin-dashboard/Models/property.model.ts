// Status enum — values match the order in the .NET enum
export enum verificationStatus {
  Pending = 0,
  AiVerified = 1,
  ManuallyVerified = 2,
  RequiresManualReview = 3,
  Rejected = 4,
}
export enum propertyStatus {
  Draft = 0,
  PendingReview = 1,
  Active = 2,
  Rented = 3,
  Inactive = 4,
  Rejected = 5,
}
export type PropertyStatusString =
  | 'Draft'
  | 'PendingReview'
  | 'Active'
  | 'Rented'
  | 'Inactive'
  | 'Rejected';

// String version (as returned from GET)
export type verificationStatusString =
  | 'Pending'
  | 'AiVerified'
  | 'ManuallyVerified'
  | 'RequiresManualReview'
  | 'Rejected';

export interface Property {
  id: string;
  ownerId: string;
  companyId: string | null;
  listingBrokerId: string | null;
  title: string;
  titleAr?: string;
  propertyType: string;
  price: number;
  bedrooms?: number;
  bathrooms?: number;
  areaSqm: number;
  isFurnished: boolean;
  petsAllowed: boolean;
  city: string;
  district: string;
  status: PropertyStatusString;
  isFeatured: boolean;
  primaryImageUrl: string | null;
  viewsCount: number;
  createdAt: string;
  amenityIds: string[];
  listingDisplayName: string;
  listingBrokerName: string | null;
}
export const LOCKED_STATUSES: PropertyStatusString[] = ['Rented', 'Draft'];

export interface PendingProperty extends Property {
  verificationStatus: verificationStatus;
  rejectionReason: string | null;
  aiDuplicateConfidence: number;
  ownershipGate: 'Pending' | 'Passed' | 'Failed';
  duplicateGate: 'Pending' | 'Passed' | 'Failed';
}

export interface UpdateVerificationRequest {
  verificationStatus: verificationStatus;
  rejectionReason?: string;
}
// 🆕 Full property detail (from GET /api/properties/{id})
export interface PropertyDetail extends Property {
  description: string;
  descriptionAr?: string;
  floorNumber?: number;
  totalFloors?: number;
  yearBuilt?: number;
  address?: string;
  apartmentNumber?: number;
  country?: string;
  governorate?: string;
  listingSource?: string;
  featuredUntil?: string | null;
  aiDuplicateConfidence?: number | null;
  verificationStatus?: string;
  rejectionReason?: string | null;
  ownershipGate?: string;
  duplicateGate?: string;
  updatedAt?: string;
  inquiriesCount: number;
  media: Array<{ id?: string; url: string; type?: string; isPrimary?: boolean }>;
  amenities: Array<{ id: string; name: string; icon?: string }>;
}
