/**
 * Company Subscription Service
 * LIVE against GET /api/subscriptions/mine and /mine/usage (requires JWT;
 * company callers resolve to the company subscription via the token's
 * CompanyId). Emits null when there's no active subscription (404) or the
 * call fails, so pages can show an explicit empty state instead of hanging.
 */

import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, delay, catchError } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { API_ENDPOINTS } from '../../../core/constants/api-endpoints';
import {
  SubscriptionInfo,
  SubscriptionUsage,
  PaymentItem,
  SubscriptionPlan,
} from '../../../shared/models/company.model';

const MOCK_SUB: SubscriptionInfo = {
  planName: 'Premium Company',
  tier: 'PremiumCompany',
  status: 'Active',
  billingCycle: 'Monthly',
  priceMonthly: 99,
  endDate: '2026-07-15',
  isAutoRenew: true,
  maxProperties: 200,
  maxBrokers: 25,
  maxAiRequestsPerMonth: 5000,
  maxStorageGb: 100,
  hasAdvancedAnalytics: true,
  hasFeaturedListings: true,
  hasAiPriceEstimation: true,
  hasAiChatbot: true,
  hasAiLeadPrioritization: true,
  hasAiDescriptionGenerator: true,
};

const MOCK_USAGE: SubscriptionUsage = {
  propertiesUsed: 147,
  brokersUsed: 23,
  aiRequestsUsed: 4521,
  storageUsedGb: 12,
  periodStart: '2026-06-01',
  periodEnd: '2026-06-30',
};

@Injectable({ providedIn: 'root' })
export class CompanySubscriptionService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;
  private useMock = false; // ← offline demo fallback; backend is live

  getActive(): Observable<SubscriptionInfo | null> {
    if (this.useMock) return of(MOCK_SUB).pipe(delay(300));
    return this.http
      .get<SubscriptionInfo>(`${this.baseUrl}${API_ENDPOINTS.SUBSCRIPTIONS.MINE}`)
      .pipe(catchError(() => of(null)));
  }

  getUsage(): Observable<SubscriptionUsage | null> {
    if (this.useMock) return of(MOCK_USAGE).pipe(delay(350));
    return this.http
      .get<SubscriptionUsage>(
        `${this.baseUrl}${API_ENDPOINTS.SUBSCRIPTIONS.MINE_USAGE}`
      )
      .pipe(catchError(() => of(null)));
  }

  /** GET /api/subscriptions/mine/payments — empty list = nothing charged yet. */
  getPayments(): Observable<PaymentItem[]> {
    if (this.useMock) return of([]).pipe(delay(300));
    return this.http
      .get<PaymentItem[]>(
        `${this.baseUrl}${API_ENDPOINTS.SUBSCRIPTIONS.MINE_PAYMENTS}`
      )
      .pipe(catchError(() => of([])));
  }

  /** GET /api/subscription-plans — public plan catalog. */
  getPlans(): Observable<SubscriptionPlan[]> {
    return this.http
      .get<SubscriptionPlan[]>(
        `${this.baseUrl}${API_ENDPOINTS.SUBSCRIPTIONS.GET_PLANS}`
      )
      .pipe(catchError(() => of([])));
  }

  /** POST /api/subscriptions/mine/upgrade — moves the plan (charging is Phase 2). */
  upgrade(planId: string): Observable<void> {
    return this.http.post<void>(
      `${this.baseUrl}${API_ENDPOINTS.SUBSCRIPTIONS.MINE_UPGRADE}`,
      { planId }
    );
  }

  /** POST /api/subscriptions/mine/cancel */
  cancel(reason: string): Observable<void> {
    return this.http.post<void>(
      `${this.baseUrl}${API_ENDPOINTS.SUBSCRIPTIONS.MINE_CANCEL}`,
      { reason }
    );
  }
}
