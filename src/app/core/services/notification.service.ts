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
    this.poll();                                   // immediate baseline + count
    this.watcherSub = interval(45_000).subscribe(() => this.poll());
  }

  stopWatcher(): void {
    this.watcherSub?.unsubscribe();
    this.watcherSub = null;
    this.seenIds.clear();
    this.baselineTaken = false;
    this._unreadCount.set(0);
  }

  private poll(): void {
    this.getNotifications(1, 10, false).subscribe(res => {
      if (!this.baselineTaken) {
        // First run after login: remember what already exists, don't toast it.
        res.items.forEach(n => this.seenIds.add(n.id));
        this.baselineTaken = true;
        return;
      }

      // Newest first; toast the unseen unread ones (cap to avoid a burst).
      const fresh = res.items.filter(n => !this.seenIds.has(n.id));
      fresh.slice(0, 3).reverse().forEach(n => {
        if (!n.isRead) this.toast.info(n.title);
      });
      fresh.forEach(n => this.seenIds.add(n.id));
    });
  }
}
