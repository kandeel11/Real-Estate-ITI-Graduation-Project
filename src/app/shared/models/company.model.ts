/**
 * Company Dashboard Models
 * camelCase fields match the backend's CompanyDto / BrokerDto JSON.
 */

export interface Company {
  id: string;
  name: string;
  commercialRegister: string;
  city: string;
  address: string;
  phone: string;
  email: string;
  isVerified: boolean;
  ratingAvg: number;
  totalRatings: number;
  // Accepted by PUT /companies/mine but not returned by CompanyDto yet
  logoUrl?: string | null;
  website?: string | null;
  description?: string | null;
}

export interface CompanyStats {
  totalProperties: number;
  activeProperties: number;
  pendingProperties: number;
  totalBrokers: number;
  monthlyViews: number;
  monthlyInquiries: number;
  monthlyDealsClosed: number;
}

export interface Broker {
  id: string;
  userId: string;
  companyId: string | null;
  licenseNumber: string;
  isActive: boolean;
  aiPerformanceScore: number;
  ratingAvg: number;
  totalRatings: number;
  // Not in the backend BrokerDto yet — present in mock data only
  fullName?: string;
  email?: string;
  phone?: string;
  closedDealsCount?: number;
  avgResponseTimeMinutes?: number;
}

/** Body for POST /companies/mine/brokers — companyId comes from the JWT. */
export interface AddBrokerRequest {
  fullName: string;
  email: string;
  tempPassword: string;
  phone: string;
  licenseNumber: string;
}

export interface SubscriptionUsage {
  propertiesUsed: number;
  brokersUsed: number;
  aiRequestsUsed: number;
  storageUsedGb: number;
  periodStart: string;
  periodEnd: string;
}

export interface SubscriptionInfo {
  planName: string;
  tier: string;
  status: string;
  endDate: string;
  isAutoRenew: boolean;
  maxProperties: number;
  maxBrokers: number;
  maxAiRequestsPerMonth: number;
  maxStorageGb: number;
}
