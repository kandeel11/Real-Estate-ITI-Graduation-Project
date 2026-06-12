import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, interval, Subscription } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
import { environment } from '../../../../../environments/environment';

export interface AppNotification {
  id: string;
  userId: string;
  type: string;
  title: string;
  body: string;
  relatedEntityType: string | null;
  relatedEntityId: string | null;
  isRead: boolean;
  readAt: string | null;
  createdAt: string;
}

export interface NotificationsResponse {
  items: AppNotification[];
  total: number;
  unreadCount: number;
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;
  private pollSub: Subscription | null = null;

  private readonly _unreadCount = signal<number>(0);
  readonly unreadCount = this._unreadCount.asReadonly();

  getNotifications(page = 1, pageSize = 20, unreadOnly = false): Observable<NotificationsResponse> {
    return this.http
      .get<NotificationsResponse>(`${this.base}/notifications`, {
        params: {
          page: page.toString(),
          pageSize: pageSize.toString(),
          unreadOnly: unreadOnly.toString(),
        },
      })
      .pipe(
        tap(res => this._unreadCount.set(res.unreadCount)),
        catchError(() => of({ items: [], total: 0, unreadCount: 0 }))
      );
  }

  fetchUnreadCount(): void {
    this.http
      .get<{ count: number }>(`${this.base}/notifications/unread-count`)
      .pipe(catchError(() => of({ count: 0 })))
      .subscribe(res => this._unreadCount.set(res.count));
  }

  markRead(id: string): Observable<void> {
    return this.http.patch<void>(`${this.base}/notifications/${id}/read`, {});
  }

  markAllRead(): Observable<void> {
    return this.http.patch<void>(`${this.base}/notifications/read-all`, null);
  }

  setUnreadCount(count: number): void {
    this._unreadCount.set(count);
  }

  startPolling(): void {
    if (this.pollSub) return;
    this.pollSub = interval(60_000).subscribe(() => this.fetchUnreadCount());
  }

  stopPolling(): void {
    this.pollSub?.unsubscribe();
    this.pollSub = null;
  }
}
