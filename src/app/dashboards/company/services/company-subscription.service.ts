/**
 * Company Subscription Service
 * FULLY MOCKED — the backend only exposes /api/subscription-plans (catalog).
 * There is no "my subscription" or usage endpoint yet. When one ships, point
 * the URLs below at it and flip `useMock` to false; until then the real-call
 * branch targets the agreed-but-unbuilt routes and falls back to mock data.
 */

import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, delay, catchError } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { API_ENDPOINTS } from '../../../core/constants/api-endpoints';
import {
  SubscriptionInfo,
  SubscriptionUsage,
} from '../../../shared/models/company.model';

const MOCK_SUB: SubscriptionInfo = {
  planName: 'Premium Company',
  tier: 'PremiumCompany',
  status: 'Active',
  endDate: '2026-07-15',
  isAutoRenew: true,
  maxProperties: 200,
  maxBrokers: 25,
  maxAiRequestsPerMonth: 5000,
  maxStorageGb: 100,
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
  private useMock = true; // ← flip to false when Identity/JWT ships

  getActive(): Observable<SubscriptionInfo> {
    if (this.useMock) return of(MOCK_SUB).pipe(delay(300));
    return this.http
      .get<SubscriptionInfo>(
        `${this.baseUrl}${API_ENDPOINTS.SUBSCRIPTIONS.GET_USER_SUBSCRIPTION}`
      )
      .pipe(catchError(() => of(MOCK_SUB)));
  }

  getUsage(): Observable<SubscriptionUsage> {
    if (this.useMock) return of(MOCK_USAGE).pipe(delay(350));
    return this.http
      .get<SubscriptionUsage>(
        `${this.baseUrl}${API_ENDPOINTS.SUBSCRIPTIONS.GET_USAGE}`
      )
      .pipe(catchError(() => of(MOCK_USAGE)));
  }
}
