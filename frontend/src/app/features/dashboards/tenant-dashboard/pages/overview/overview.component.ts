import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SubscriptionStatusCardComponent } from '../../../../subscription/components/subscription-status-card/subscription-status-card.component';
import { DatePipe, CurrencyPipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { forkJoin, finalize } from 'rxjs';
import { AuthService } from '../../../../../core/services/auth.service';
import { LeaseService } from '../../services/lease.service';
import { NotificationService } from '../../services/notification.service';
import { ActivityItem, ApiActiveLease, ApiTicketStats, DashboardService } from '../../services/dashboard.service';

interface ActiveLeaseView extends ApiActiveLease {
  propertyAddress: string;
}

@Component({
  selector: 'app-overview',
  standalone: true,
  imports: [RouterLink, DatePipe, CurrencyPipe, SubscriptionStatusCardComponent, TranslatePipe],
  templateUrl: './overview.component.html',
  styleUrl: './overview.component.css',
})
export class OverviewComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly leaseService = inject(LeaseService);
  protected readonly notifService = inject(NotificationService);
  private readonly dashboardService = inject(DashboardService);

  protected loading = signal(true);
  protected error = signal<string | null>(null);

  protected tenantName = computed(() => this.auth.user()?.fullName ?? 'Tenant');

  protected activeLease = signal<ActiveLeaseView | null>(null);
  protected ticketStats = signal<ApiTicketStats | null>(null);
  protected propertyCount = signal(0);
  protected inquiries = signal<{ pending: number; total: number } | null>(null);
  protected activity = signal<ActivityItem[]>([]);
  protected today = new Date();

  get greeting(): string {
    const h = new Date().getHours();
    if (h < 12) return 'Good morning';
    if (h < 17) return 'Good afternoon';
    return 'Good evening';
  }

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.loading.set(true);
    this.error.set(null);

    forkJoin({
      summary: this.dashboardService.getDashboardSummary(),
      properties: this.leaseService.getRentedProperties(),
    })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: ({ summary, properties }) => {
          this.propertyCount.set(properties.length);
          this.ticketStats.set(summary.ticketStats);
          this.inquiries.set(summary.inquiries);

          if (summary.activeLease) {
            const prop = properties.find(p => p.leaseId === summary.activeLease!.leaseId);
            const parts = prop
              ? [prop.property.address, prop.property.district, prop.property.city].filter(Boolean)
              : [`Property ${summary.activeLease.propertyId.slice(-6)}`];

            this.activeLease.set({
              ...summary.activeLease,
              propertyAddress: parts.join(', '),
            });
          }
        },
        error: () => this.error.set('Failed to load dashboard data. Please try again.'),
      });

    // TODO: replace with real API call
    this.dashboardService.getActivityFeed(5).subscribe({
      next: data => this.activity.set(data),
    });
  }

  leaseElapsedPercent(lease: ActiveLeaseView): number {
    const start = new Date(lease.startDate).getTime();
    const end = new Date(lease.endDate).getTime();
    return Math.min(100, Math.max(0, Math.round(((Date.now() - start) / (end - start)) * 100)));
  }

  expiryClass(days: number): string {
    if (days <= 30) return 'expiry-critical';
    if (days <= 60) return 'expiry-warning';
    return 'expiry-safe';
  }

  ticketBarWidth(count: number, total: number): string {
    return total ? `${Math.round((count / total) * 100)}%` : '0%';
  }

  activityIcon(type: string): string {
    const icons: Record<string, string> = {
      maintenance: 'fa-solid fa-screwdriver-wrench',
      lease: 'fa-solid fa-file-contract',
      payment: 'fa-solid fa-credit-card',
      notification: 'fa-regular fa-bell',
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
}
