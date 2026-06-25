export type PlanTier = 'FreeTenant' | 'PremiumTenant';

/** Subset of the backend MySubscriptionDto needed to mark the current plan. */
export interface ActiveSubscription {
  planName: string;
  tier: string;
  status: string;
}

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
