export type ListingType = 'For Rent' | 'For Sale' | string;

export interface PropertyResult {
  propertyId: string;
  title: string;
  propertyType: string;
  city: string;
  district: string;
  areaInMeters: number;
  bedrooms: number;
  bathrooms: number;
  price: number;
  listingType: ListingType;
  description: string;
  mainImageUrl: string | null;
  /** All property images. mainImageUrl is the first; remaining are extras. */
  imageUrls?: string[];
  matchScore: number;
}
