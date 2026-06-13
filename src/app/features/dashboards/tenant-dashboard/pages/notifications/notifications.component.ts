import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { TranslatePipe } from '@ngx-translate/core';
import { AppNotification, NotificationService } from '../../services/notification.service';
import { ToastService } from '../../../../../shared/components/toast/toast.service';

const PAGE_SIZE = 20;

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [TranslatePipe],
  templateUrl: './notifications.component.html',
  styleUrl: './notifications.component.css',
})
export class NotificationsComponent implements OnInit {
  private readonly notifService = inject(NotificationService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  protected loading = signal(true);
  protected error = signal<string | null>(null);
  protected notifications = signal<AppNotification[]>([]);
  protected filter = signal<'all' | 'unread'>('all');
  protected page = signal(1);
  protected total = signal(0);
  protected unreadCount = signal(0);
  protected markingAll = signal(false);

  protected readonly totalPages = computed(() => Math.max(1, Math.ceil(this.total() / PAGE_SIZE)));
  protected readonly hasPrev = computed(() => this.page() > 1);
  protected readonly hasNext = computed(() => this.page() < this.totalPages());
  protected readonly showPagination = computed(() => this.total() > PAGE_SIZE);

  ngOnInit(): void {
    this.loadNotifications();
  }

  loadNotifications(reset = true): void {
    if (reset) {
      this.loading.set(true);
      this.error.set(null);
    }

    this.notifService
      .getNotifications(this.page(), PAGE_SIZE, this.filter() === 'unread')
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: res => {
          this.notifications.set(res.items);
          this.total.set(res.total);
          this.unreadCount.set(res.unreadCount);
        },
        error: () => this.error.set('Failed to load notifications. Please try again.'),
      });
  }

  setFilter(f: 'all' | 'unread'): void {
    if (this.filter() === f) return;
    this.filter.set(f);
    this.page.set(1);
    this.loadNotifications();
  }

  goToPage(p: number): void {
    this.page.set(p);
    this.loadNotifications();
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  prevPage(): void {
    if (this.hasPrev()) this.goToPage(this.page() - 1);
  }

  nextPage(): void {
    if (this.hasNext()) this.goToPage(this.page() + 1);
  }

  markAllRead(): void {
    if (this.markingAll() || this.unreadCount() === 0) return;
    this.markingAll.set(true);

    const prevNotifications = this.notifications();
    const prevCount = this.unreadCount();

    this.notifications.update(cur => cur.map(n => ({ ...n, isRead: true })));
    this.unreadCount.set(0);
    this.notifService.setUnreadCount(0);

    this.notifService
      .markAllRead()
      .pipe(finalize(() => this.markingAll.set(false)))
      .subscribe({
        next: () => this.toast.success('All notifications marked as read'),
        error: () => {
          this.notifications.set(prevNotifications);
          this.unreadCount.set(prevCount);
          this.notifService.setUnreadCount(prevCount);
          this.toast.error('Failed to mark all as read. Please try again.');
        },
      });
  }

  markRead(notif: AppNotification): void {
    if (!notif.isRead) {
      const prevCount = this.unreadCount();

      this.notifications.update(cur =>
        cur.map(n => (n.id === notif.id ? { ...n, isRead: true } : n))
      );
      this.unreadCount.update(c => Math.max(0, c - 1));
      this.notifService.setUnreadCount(Math.max(0, prevCount - 1));

      this.notifService.markRead(notif.id).subscribe({
        error: () => {
          this.notifications.update(cur =>
            cur.map(n => (n.id === notif.id ? { ...n, isRead: false } : n))
          );
          this.unreadCount.set(prevCount);
          this.notifService.setUnreadCount(prevCount);
        },
      });
    }

    this.navigateToEntity(notif);
  }

  private navigateToEntity(notif: AppNotification): void {
    if (!notif.relatedEntityType || !notif.relatedEntityId) return;
    switch (notif.relatedEntityType) {
      case 'Ticket':
        this.router.navigate(['/dashboard/maintenance', notif.relatedEntityId]);
        break;
      case 'Lease':
        this.router.navigate(['/dashboard/lease']);
        break;
      case 'Payment':
        this.router.navigate(['/dashboard/maintenance', notif.relatedEntityId]);
        break;
    }
  }

  notifIcon(type: string): string {
    const map: Record<string, string> = {
      MaintenanceUpdate: 'fa-solid fa-wrench',
      AiReviewResult: 'fa-solid fa-robot',
      PaymentReceived: 'fa-solid fa-credit-card',
      ContractSigned: 'fa-solid fa-file-signature',
      RentPaymentReminder: 'fa-solid fa-bell',
      NewInquiry: 'fa-solid fa-envelope',
      SubscriptionExpiration: 'fa-solid fa-calendar-xmark',
      VerificationResult: 'fa-solid fa-shield-halved',
      DuplicateDetected: 'fa-solid fa-triangle-exclamation',
    };
    return map[type] ?? 'fa-regular fa-bell';
  }

  notifColorClass(type: string): string {
    const map: Record<string, string> = {
      MaintenanceUpdate: 'notif-icon-wrap icon-primary',
      AiReviewResult: 'notif-icon-wrap icon-info',
      PaymentReceived: 'notif-icon-wrap icon-success',
      ContractSigned: 'notif-icon-wrap icon-success',
      RentPaymentReminder: 'notif-icon-wrap icon-warning',
      NewInquiry: 'notif-icon-wrap icon-primary',
      SubscriptionExpiration: 'notif-icon-wrap icon-danger',
      VerificationResult: 'notif-icon-wrap icon-info',
      DuplicateDetected: 'notif-icon-wrap icon-warning',
    };
    return map[type] ?? 'notif-icon-wrap icon-primary';
  }

  timeAgo(dateStr: string): string {
    const sec = Math.floor((Date.now() - new Date(dateStr).getTime()) / 1000);
    if (sec < 60) return 'just now';
    const min = Math.floor(sec / 60);
    if (min < 60) return `${min} minute${min !== 1 ? 's' : ''} ago`;
    const hr = Math.floor(min / 60);
    if (hr < 24) return `${hr} hour${hr !== 1 ? 's' : ''} ago`;
    const day = Math.floor(hr / 24);
    if (day < 7) return `${day} day${day !== 1 ? 's' : ''} ago`;
    const wk = Math.floor(day / 7);
    return `${wk} week${wk !== 1 ? 's' : ''} ago`;
  }
}
