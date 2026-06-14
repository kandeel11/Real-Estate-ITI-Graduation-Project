import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, of, timeout } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface BrokerPlan {
  id: string;
  name: string;
  tier: string;
  priceMonthly: number;
  priceAnnual: number;
  maxProperties: number;
  maxAiRequestsPerMonth: number;
  isActive: boolean;
}

export interface BrokerActiveSubscription {
  planName: string;
  tier: string;
  status: string;
  priceMonthly: number;
  endDate: string;
  isAutoRenew: boolean;
  maxProperties: number;
  maxAiRequestsPerMonth: number;
}

@Injectable({ providedIn: 'root' })
export class BrokerSubscriptionService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  getPlans(): Observable<BrokerPlan[]> {
    return this.http.get<BrokerPlan[]>(`${this.base}/subscription-plans`).pipe(
      timeout(10000),
      catchError(() => of([]))
    );
  }

  getActive(): Observable<BrokerActiveSubscription | null> {
    return this.http.get<BrokerActiveSubscription>(`${this.base}/subscriptions/mine`).pipe(
      timeout(10000),
      catchError(() => of(null))
    );
  }

  upgrade(planId: string): Observable<unknown> {
    return this.http.post(`${this.base}/subscriptions/mine/upgrade`, { planId });
  }
}
