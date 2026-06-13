/**
 * Company Dashboard Models
 * camelCase fields match the backend's CompanyDto / BrokerDto JSON.
 */

/** Matches the backend's CompanyDetailsDto (GET /api/companies/mine). */
export interface Company {
  id: string;
  managerId?: string;
  name: string;
  commercialRegister: string;
  logoUrl: string | null;
  website: string | null;
  description: string | null;
  city: string;
  address: string;
  phone: string;
  email: string;
  isVerified: boolean;
  isActive?: boolean;
  ratingAvg: number;
  totalRatings: number;
  createdAt?: string;
}

/** Matches the backend's CompanyStatsResponse (GET /api/companies/mine/stats). */
export interface CompanyStats {
  totalProperties: number;
  activeProperties: number;
  pendingProperties: number;
  rentedProperties: number;
  totalBrokers: number;
  totalViews: number;
  totalInquiries: number;
  monthlyInquiries: number;
  dealsClosed: number;
}

/** Matches the backend's BrokerDetailsDto (GET /api/companies/mine/brokers). */
export interface Broker {
  id: string;
  userId: string;
  companyId: string | null;
  fullName: string;
  email: string;
  phone: string;
  profileImageUrl?: string | null;
  licenseNumber: string;
  isActive: boolean;
  aiPerformanceScore: number;
  closedDealsCount: number;
  avgResponseTimeMinutes: number;
  conversionRate?: number;
  ratingAvg: number;
  totalRatings: number;
  createdAt?: string;
}

/** Matches the backend's CompanyActivityItem (GET /api/companies/mine/activity). */
export interface ActivityItem {
  type: string;
  text: string;
  timestamp: string;
}

/** Matches the backend's CompanyInquiryResponse (GET /api/companies/mine/inquiries). */
export interface CompanyInquiry {
  id: string;
  propertyId: string;
  propertyTitle: string;
  tenantId: string;
  tenantName: string;
  brokerId: string | null;
  brokerName: string | null;
  status: string;
  message: string;
  createdAt: string;
  acceptedAt: string | null;
  closedAt: string | null;
}

/** Matches the backend's SubscriptionPlanDto (GET /api/subscription-plans). */
export interface SubscriptionPlan {
  id: string;
  name: string;
  priceMonthly: number;
  priceAnnual: number;
  maxProperties: number;
  maxBrokers: number;
  maxAiRequestsPerMonth: number;
  maxStorageGb: number;
  isActive: boolean;
}

/** Matches the backend's CompanyRatingSummaryDto (GET /api/companies/mine/ratings-summary). */
export interface RatingSummary {
  total: number;
  average: number;
  buckets: { stars: number; count: number; percent: number }[];
}

/** Matches the backend's SubscriptionPaymentItemDto (GET /api/subscriptions/mine/payments). */
export interface PaymentItem {
  id: string;
  amount: number;
  currency: string;
  paymentMethod: string;
  status: string;
  paidAt: string | null;
  dueDate: string;
  invoiceUrl: string | null;
  createdAt: string;
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

/** Matches the backend's MySubscriptionDto (GET /api/subscriptions/mine). */
export interface SubscriptionInfo {
  planName: string;
  tier: string;
  status: string;
  billingCycle?: string;
  priceMonthly: number;
  startDate?: string;
  endDate: string;
  isAutoRenew: boolean;
  maxProperties: number;
  maxBrokers: number;
  maxAiRequestsPerMonth: number;
  maxStorageGb: number;
  hasAdvancedAnalytics?: boolean;
  hasFeaturedListings?: boolean;
  hasAiPriceEstimation?: boolean;
  hasAiChatbot?: boolean;
  hasAiLeadPrioritization?: boolean;
  hasAiDescriptionGenerator?: boolean;
}
