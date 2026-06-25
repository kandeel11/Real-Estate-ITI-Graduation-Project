import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, of, timeout } from 'rxjs';
import { environment } from '../../../../../environments/environment';

/** tier: 0=FreeIndividual, 1=PremiumIndividual, 2=FreeCompany, 3=PremiumCompany, 4=FreeTenant, 5=PremiumTenant */
export interface BrokerPlan {
  id: string;
  name: string;
  tier: number;
  priceMonthly: number;
  priceAnnual: number;
  maxProperties: number;
  maxBrokers: number;
  maxAiRequestsPerMonth: number;
  maxStorageGb: number;
  hasAdvancedAnalytics: boolean;
  hasFeaturedListings: boolean;
  hasAiPriceEstimation: boolean;
  hasAiChatbot: boolean;
  hasAiLeadPrioritization: boolean;
  hasAiDescriptionGenerator: boolean;
  isActive: boolean;
}

export interface BrokerActiveSubscription {
  id: string;
  planName: string;
  tier: string;
  status: string;
  priceMonthly: number;
  endDate: string;
  isAutoRenew: boolean;
  maxProperties: number;
  maxBrokers: number;
  maxAiRequestsPerMonth: number;
  maxStorageGb: number;
  // Feature flags — for a company broker, /subscriptions/mine resolves to the
  // COMPANY plan, so these reflect the features the broker inherits.
  hasAdvancedAnalytics: boolean;
  hasFeaturedListings: boolean;
  hasAiPriceEstimation: boolean;
  hasAiChatbot: boolean;
  hasAiLeadPrioritization: boolean;
  hasAiDescriptionGenerator: boolean;
}

/** Matches the backend's usage DTO (GET /api/subscriptions/mine/usage). */
export interface BrokerUsage {
  propertiesUsed: number;
  brokersUsed: number;
  aiRequestsUsed: number;
  storageUsedGb: number;
  periodStart: string;
  periodEnd: string;
}

@Injectable({ providedIn: 'root' })
export class BrokerSubscriptionService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  getPlans(): Observable<BrokerPlan[]> {
    return this.http
      .get<BrokerPlan[]>(`${this.base}/subscription-plans`)
      .pipe(timeout(10000), catchError(() => of([])));
  }

  getActive(): Observable<BrokerActiveSubscription | null> {
    return this.http
      .get<BrokerActiveSubscription>(`${this.base}/subscriptions/mine`)
      .pipe(timeout(10000), catchError(() => of(null)));
  }

  /** Shared usage for the resolved subscription (company pool for company brokers). */
  getUsage(): Observable<BrokerUsage | null> {
    return this.http
      .get<BrokerUsage>(`${this.base}/subscriptions/mine/usage`)
      .pipe(timeout(10000), catchError(() => of(null)));
  }
}
