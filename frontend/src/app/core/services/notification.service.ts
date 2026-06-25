import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, interval, Subscription } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ToastService } from '../../shared/components/toast/toast.service';
import { NotificationRealtimeService } from './notification-realtime.service';

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

/**
 * App-wide notification service. Works for ANY authenticated user (tenant,
 * broker, company manager, admin) since the backend scopes /notifications by
 * the JWT. Also runs a global watcher that raises a toast whenever a new
 * notification arrives, for every signed-in user.
 */
@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly http = inject(HttpClient);
  private readonly toast = inject(ToastService);
  private readonly realtime = inject(NotificationRealtimeService);
  private readonly base = environment.apiUrl;

  private readonly _unreadCount = signal<number>(0);
  readonly unreadCount = this._unreadCount.asReadonly();

  private watcherSub: Subscription | null = null;
  private realtimeSub: Subscription | null = null;
  /** Ids already seen by the watcher — used to detect genuinely new arrivals. */
  private seenIds = new Set<string>();
  private baselineTaken = false;

  // ── HTTP ──────────────────────────────────────────────────────────────────

  getNotifications(page = 1, pageSize = 20, unreadOnly = false): Observable<NotificationsResponse> {
    return this.http
      .get<NotificationsResponse>(`${this.base}/notifications`, {
        params: { page: page.toString(), pageSize: pageSize.toString(), unreadOnly: unreadOnly.toString() },
      })
      .pipe(
        tap(res => this._unreadCount.set(res.unreadCount)),
        catchError(() => of({ items: [], total: 0, unreadCount: 0 })),
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

  // ── Global watcher (toast on new notification, for all users) ───────────────

  startWatcher(): void {
    if (this.watcherSub) return;

    // Take baseline count on first load (don't toast existing notifications).
    this.poll();

    // Primary: real-time push via SignalR hub.
    this.realtimeSub = this.realtime.start().subscribe(n => {
      if (this.seenIds.has(n.id)) return;
      this.seenIds.add(n.id);
      this._unreadCount.update(c => c + 1);
      this.toast.info(n.title);
    });

    // Fallback: sync unread count every 2 min in case the hub is unavailable.
    this.watcherSub = interval(120_000).subscribe(() => this.syncCount());
  }

  stopWatcher(): void {
    this.watcherSub?.unsubscribe();
    this.watcherSub = null;
    this.realtimeSub?.unsubscribe();
    this.realtimeSub = null;
    this.realtime.stop();
    this.seenIds.clear();
    this.baselineTaken = false;
    this._unreadCount.set(0);
  }

  private poll(): void {
    this.getNotifications(1, 10, false).subscribe(res => {
      if (!this.baselineTaken) {
        res.items.forEach(n => this.seenIds.add(n.id));
        this.baselineTaken = true;
      }
    });
  }

  private syncCount(): void {
    this.http
      .get<{ count: number }>(`${this.base}/notifications/unread-count`)
      .pipe(catchError(() => of({ count: 0 })))
      .subscribe(res => this._unreadCount.set(res.count));
  }
}
