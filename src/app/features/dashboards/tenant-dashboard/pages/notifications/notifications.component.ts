import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AppNotification, NotificationService } from '../../services/notification.service';

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [],
  templateUrl: './notifications.component.html',
  styleUrl: './notifications.component.css',
})
export class NotificationsComponent implements OnInit {
  private readonly notifService = inject(NotificationService);
  private readonly router = inject(Router);

  protected loading = signal(true);
  protected error = signal<string | null>(null);
  protected notifications = signal<AppNotification[]>([]);
  protected filter = signal<'all' | 'unread'>('all');
  protected page = signal(1);
  protected total = signal(0);
  protected markingAll = signal(false);

  protected readonly filtered = computed(() => {
    const f = this.filter();
    return f === 'unread'
      ? this.notifications().filter(n => !n.isRead)
      : this.notifications();
  });

  ngOnInit(): void {
    this.loadNotifications();
  }

  loadNotifications(reset = true): void {
    if (reset) {
      this.loading.set(true);
      this.error.set(null);
    }

    this.notifService
      .getNotifications(this.page(), 20)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: res => {
          const incoming = res.items;
          if (reset) {
            this.notifications.set(incoming);
          } else {
            this.notifications.update(cur => [...cur, ...incoming]);
          }
          this.total.set(res.total);
        },
        error: () => this.error.set('Failed to load notifications. Please try again.'),
      });
  }

  loadMore(): void {
    this.page.update(p => p + 1);
    this.loadNotifications(false);
  }

  markAllRead(): void {
    this.markingAll.set(true);
    this.notifService
      .markAllRead()
      .pipe(finalize(() => this.markingAll.set(false)))
      .subscribe({
        next: () => {
          this.notifications.update(cur => cur.map(n => ({ ...n, isRead: true })));
        },
      });
  }

  markRead(notif: AppNotification): void {
    if (notif.isRead) return;
    this.notifService.markRead(notif.id).subscribe({
      next: () => {
        this.notifications.update(cur =>
          cur.map(n => n.id === notif.id ? { ...n, isRead: true } : n)
        );
      },
    });
    this.navigateToEntity(notif);
  }

  private navigateToEntity(notif: AppNotification): void {
    const routes: Record<string, string> = {
      maintenance: '/dashboard/maintenance',
      lease:       '/dashboard/lease',
      payment:     '/dashboard/profile',
      rating:      '/dashboard/ratings',
    };
    const route = routes[notif.type];
    if (route) this.router.navigate([route]);
  }

  notifIcon(type: string): string {
    const icons: Record<string, string> = {
      maintenance: 'fa-solid fa-screwdriver-wrench',
      lease:       'fa-solid fa-file-contract',
      payment:     'fa-solid fa-credit-card',
      rating:      'fa-regular fa-star',
      general:     'fa-regular fa-bell',
    };
    return icons[type] ?? 'fa-regular fa-bell';
  }

  timeAgo(dateStr: string): string {
    const s = Math.floor((Date.now() - new Date(dateStr).getTime()) / 1000);
    if (s < 60) return 'just now';
    const m = Math.floor(s / 60);
    if (m < 60) return `${m}m ago`;
    const h = Math.floor(m / 60);
    if (h < 24) return `${h}h ago`;
    return `${Math.floor(h / 24)}d ago`;
  }

  get hasMore(): boolean {
    return this.notifications().length < this.total();
  }

  get unreadCount(): number {
    return this.notifications().filter(n => !n.isRead).length;
  }
}
