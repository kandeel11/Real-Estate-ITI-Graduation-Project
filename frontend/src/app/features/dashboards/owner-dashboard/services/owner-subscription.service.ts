import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, of, timeout } from 'rxjs';
import { environment } from '../../../../../environments/environment';

// Re-export so the owner component can import from one place
export type { BrokerPlan, BrokerActiveSubscription } from '../../broker/services/broker-subscription.service';

import type { BrokerPlan, BrokerActiveSubscription } from '../../broker/services/broker-subscription.service';

@Injectable({ providedIn: 'root' })
export class OwnerSubscriptionService {
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

  /** POST /api/subscriptions/mine/cancel — drops the owner back to the Free plan. */
  cancel(reason: string): Observable<void> {
    return this.http.post<void>(`${this.base}/subscriptions/mine/cancel`, { reason });
  }
}
