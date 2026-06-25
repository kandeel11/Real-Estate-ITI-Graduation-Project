import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, catchError } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { API_ENDPOINTS } from '../../../../core/constants/api-endpoints';
import {
  SubscriptionInfo,
  SubscriptionUsage,
  PaymentItem,
  SubscriptionPlan,
} from '../../../../shared/models/company.model';

@Injectable({ providedIn: 'root' })
export class CompanySubscriptionService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;

  /** GET /api/subscriptions/mine */
  getActive(): Observable<SubscriptionInfo | null> {
    return this.http
      .get<SubscriptionInfo>(`${this.baseUrl}${API_ENDPOINTS.SUBSCRIPTIONS.MINE}`)
      .pipe(catchError(() => of(null)));
  }

  /** GET /api/subscriptions/mine/usage */
  getUsage(): Observable<SubscriptionUsage | null> {
    return this.http
      .get<SubscriptionUsage>(`${this.baseUrl}${API_ENDPOINTS.SUBSCRIPTIONS.MINE_USAGE}`)
      .pipe(catchError(() => of(null)));
  }

  /** GET /api/subscriptions/mine/payments */
  getPayments(): Observable<PaymentItem[]> {
    return this.http
      .get<PaymentItem[]>(`${this.baseUrl}${API_ENDPOINTS.SUBSCRIPTIONS.MINE_PAYMENTS}`)
      .pipe(catchError(() => of([])));
  }

  /** GET /api/subscription-plans */
  getPlans(): Observable<SubscriptionPlan[]> {
    return this.http
      .get<SubscriptionPlan[]>(`${this.baseUrl}${API_ENDPOINTS.SUBSCRIPTIONS.GET_PLANS}`)
      .pipe(catchError(() => of([])));
  }

  /** POST /api/subscriptions/mine/upgrade */
  upgrade(planId: string): Observable<void> {
    return this.http.post<void>(
      `${this.baseUrl}${API_ENDPOINTS.SUBSCRIPTIONS.MINE_UPGRADE}`,
      { planId },
    );
  }

  /** POST /api/subscriptions/mine/cancel */
  cancel(reason: string): Observable<void> {
    return this.http.post<void>(
      `${this.baseUrl}${API_ENDPOINTS.SUBSCRIPTIONS.MINE_CANCEL}`,
      { reason },
    );
  }
}
