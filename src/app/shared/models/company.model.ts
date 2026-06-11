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
