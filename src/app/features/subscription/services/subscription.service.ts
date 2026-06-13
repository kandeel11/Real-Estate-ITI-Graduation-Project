import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { SubscriptionPlan } from '../models/subscription.models';

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
}
