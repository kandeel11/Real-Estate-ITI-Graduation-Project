export interface ParsedQuery {
  propertyType: string | null;
  city: string | null;
  district: string | null;
  nearbyLandmark: string | null;
  bedrooms: number | null;
  bathrooms: number | null;
  minArea: number | null;
  maxArea: number | null;
  floor: number | null;
  minPrice: number | null;
  maxPrice: number | null;
  amenities: string[];
  confidence: number;
  detectedLanguage: string;
}
