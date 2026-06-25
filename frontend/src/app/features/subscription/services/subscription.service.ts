import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, catchError } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { SubscriptionPlan, ActiveSubscription } from '../models/subscription.models';

@Injectable({ providedIn: 'root' })
export class SubscriptionService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  getActivePlans(): Observable<SubscriptionPlan[]> {
    return this.http.get<SubscriptionPlan[]>(`${this.base}/subscription-plans`);
  }

  getPlanById(id: string): Observable<SubscriptionPlan> {
    return this.http.get<SubscriptionPlan>(`${this.base}/subscription-plans/${id}`);
  }

  /** The caller's active subscription (null if none/unauthenticated). */
  getMine(): Observable<ActiveSubscription | null> {
    return this.http
      .get<ActiveSubscription>(`${this.base}/subscriptions/mine`)
      .pipe(catchError(() => of(null)));
  }

  /** Cancel the premium subscription — drops back to the Free tier. */
  cancel(reason: string): Observable<void> {
    return this.http.post<void>(`${this.base}/subscriptions/mine/cancel`, { reason });
  }
}
