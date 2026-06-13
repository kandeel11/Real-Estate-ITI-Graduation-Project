export type PlanTier = 'FreeTenant' | 'PremiumTenant';

export interface SubscriptionPlan {
  id: string;
  name: string;
  tier: PlanTier;
  priceMonthly: number;
  priceAnnual: number;
  maxAiRequestsPerMonth: number;
  hasAiPriceEstimation: boolean;
  hasAiChatbot: boolean;
  maxSavedListings: number;
  maxSavedSearches: number;
  maxInquiriesPerMonth: number;
  hasEarlyAccessListings: boolean;
  hasPriorityBrokerResponse: boolean;
  isActive: boolean;
  createdAt: string;
}
