import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe, CurrencyPipe } from '@angular/common';
import { finalize } from 'rxjs';
import { ActiveLeaseInfo, ActivityItem, DashboardService, DashboardSummary } from '../../services/dashboard.service';

@Component({
  selector: 'app-overview',
  standalone: true,
  imports: [RouterLink, DatePipe, CurrencyPipe],
  templateUrl: './overview.component.html',
  styleUrl: './overview.component.css',
})
export class OverviewComponent implements OnInit {
  private readonly dashboardService = inject(DashboardService);
  protected loading = signal(true);
  protected error = signal<string | null>(null);
  protected summary = signal<DashboardSummary | null>(null);
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

    this.dashboardService
      .getDashboardSummary()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: data => this.summary.set(data),
        error: () => this.error.set('Failed to load dashboard data. Please try again.'),
      });

    this.dashboardService.getActivityFeed(5).subscribe({
      next: data => this.activity.set(data),
    });
  }

  leaseElapsedPercent(lease: ActiveLeaseInfo): number {
    const start = new Date(lease.startDate).getTime();
    const end = new Date(lease.endDate).getTime();
    const now = Date.now();
    return Math.min(100, Math.max(0, Math.round(((now - start) / (end - start)) * 100)));
  }

  ticketBarWidth(count: number, total: number): string {
    return total ? `${Math.round((count / total) * 100)}%` : '0%';
  }

  expiryClass(days: number): string {
    if (days <= 30) return 'expiry-critical';
    if (days <= 60) return 'expiry-warning';
    return 'expiry-safe';
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

  downloadContract(): void {
    // Contract download is handled in the My Lease page
  }
}
