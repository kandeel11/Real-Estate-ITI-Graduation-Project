export type PropertyApprovalStatus = 'pending' | 'approved' | 'rejected';

export interface PendingProperty {
  id: string;
  title: string;
  description?: string;
  propertyType: string;
  listingType: 'For Sale' | 'For Rent';
  price: number;
  city: string;
  district: string;
  areaInMeters: number;
  bedrooms?: number;
  bathrooms?: number;
  mainImageUrl?: string;
  ownerName: string;
  ownerAvatar?: string;
  companyName?: string;
  submittedAt: string;
  status: PropertyApprovalStatus;
}
