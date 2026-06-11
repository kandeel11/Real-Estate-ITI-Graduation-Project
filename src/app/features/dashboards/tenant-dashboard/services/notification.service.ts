import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, catchError } from 'rxjs';
import { environment } from '../../../../../environments/environment';

export type NotificationType = 'maintenance' | 'lease' | 'payment' | 'general' | 'rating';

export interface AppNotification {
  id: string;
  type: NotificationType;
  title: string;
  body: string;
  isRead: boolean;
  createdAt: string;
  relatedEntityId?: string;
  relatedEntityType?: string;
}

export interface NotificationsResponse {
  items: AppNotification[];
  total: number;
  unreadCount: number;
}

const MOCK_NOTIFICATIONS: AppNotification[] = [
  { id: 'n-001', type: 'maintenance', title: 'Maintenance ticket updated', body: 'Your HVAC ticket (TCK-0001) has been assigned to a technician.', isRead: false, createdAt: new Date(Date.now() - 7200000).toISOString() },
  { id: 'n-002', type: 'lease', title: 'Lease renewal reminder', body: 'Your lease expires in 45 days. Contact your landlord to discuss renewal options.', isRead: false, createdAt: new Date(Date.now() - 86400000).toISOString() },
  { id: 'n-003', type: 'general', title: 'Welcome to your tenant portal', body: 'Your account has been set up. Explore all features available to you.', isRead: false, createdAt: new Date(Date.now() - 172800000).toISOString() },
  { id: 'n-004', type: 'maintenance', title: 'Ticket resolved', body: 'Your ticket #TCK-0004 (Broken blinds) has been marked as completed.', isRead: true, createdAt: new Date(Date.now() - 259200000).toISOString() },
  { id: 'n-005', type: 'payment', title: 'Rent payment confirmed', body: 'Your rent payment of $2,800 for this month has been confirmed.', isRead: true, createdAt: new Date(Date.now() - 604800000).toISOString() },
];

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  private readonly _unreadCount = signal<number>(3);
  readonly unreadCount = this._unreadCount.asReadonly();

  getNotifications(page = 1, pageSize = 20, unreadOnly = false): Observable<NotificationsResponse> {
    // TODO: replace with real API call
    const items = unreadOnly ? MOCK_NOTIFICATIONS.filter(n => !n.isRead) : MOCK_NOTIFICATIONS;
    return of({ items, total: items.length, unreadCount: MOCK_NOTIFICATIONS.filter(n => !n.isRead).length });
    // return this.http.get<NotificationsResponse>(`${this.base}/notifications`, {
    //   params: { page, pageSize, ...(unreadOnly ? { unreadOnly: true } : {}) }
    // }).pipe(catchError(() => of({ items: [], total: 0, unreadCount: 0 })));
  }

  markAllRead(): Observable<void> {
    this._unreadCount.set(0);
    // TODO: replace with real API call
    return of(undefined);
    // return this.http.patch<void>(`${this.base}/notifications/read-all`, {});
  }

  markRead(id: string): Observable<void> {
    this._unreadCount.update(c => Math.max(0, c - 1));
    // TODO: replace with real API call
    return of(undefined);
    // return this.http.patch<void>(`${this.base}/notifications/${id}/read`, {});
  }
}
