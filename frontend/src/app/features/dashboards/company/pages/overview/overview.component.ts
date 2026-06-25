/**
 * Company Dashboard Overview Page
 * Stat cards, subscription usage meters, top brokers, and activity feed —
 * all from live endpoints (/companies/mine/*, /subscriptions/mine*).
 */

import { Component, inject, computed } from '@angular/core';
import { DecimalPipe, DatePipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { CompanyService } from '../../services/company.service';
import { CompanySubscriptionService } from '../../services/company-subscription.service';
import { UsageMeterComponent } from '../../../../../shared/components/usage-meter/usage-meter.component';
import { OverLimitBannerComponent } from '../../../../../shared/components/over-limit-banner/over-limit-banner.component';
import { Broker, ActivityItem } from '../../../../../shared/models/company.model';

const ACTIVITY_ICONS: Record<string, string> = {
  listing_created: 'fa-house text-primary',
  broker_joined: 'fa-user-plus text-secondary',
  inquiry_received: 'fa-envelope text-primary',
};

@Component({
  selector: 'app-company-overview',
  standalone: true,
  imports: [DecimalPipe, DatePipe, UsageMeterComponent, OverLimitBannerComponent, TranslatePipe],
  templateUrl: './overview.component.html',
  styleUrl: './overview.component.css',
})
export class OverviewComponent {
  private companySvc = inject(CompanyService);
  private subSvc = inject(CompanySubscriptionService);

  company = toSignal(this.companySvc.getMyCompany());
  stats = toSignal(this.companySvc.getStats());
  subscription = toSignal(this.subSvc.getActive());
  usage = toSignal(this.subSvc.getUsage());
  brokers = toSignal(this.companySvc.getBrokers(), {
    initialValue: [] as Broker[],
  });
  activity = toSignal(this.companySvc.getActivity(), {
    initialValue: [] as ActivityItem[],
  });

  topBrokers = computed(() =>
    [...this.brokers()]
      .filter((b) => b.isActive)
      .sort((a, b) => (b.closedDealsCount ?? 0) - (a.closedDealsCount ?? 0))
      .slice(0, 3)
  );

  // ── Property-status statistics (donut + legend) ─────────────────
  statusBreakdown = computed(() => {
    const s = this.stats();
    if (!s) return [];
    const other = Math.max(
      s.totalProperties - s.activeProperties - s.pendingProperties - s.rentedProperties,
      0
    );
    const total = s.totalProperties || 0;
    const pct = (n: number) => (total ? Math.round((n / total) * 100) : 0);
    return [
      { label: 'Active', count: s.activeProperties, color: '#28a745', percent: pct(s.activeProperties) },
      { label: 'Pending', count: s.pendingProperties, color: '#ffc107', percent: pct(s.pendingProperties) },
      { label: 'Rented', count: s.rentedProperties, color: '#0d6efd', percent: pct(s.rentedProperties) },
      { label: 'Draft / other', count: other, color: '#adb5bd', percent: pct(other) },
    ];
  });

  donutGradient = computed(() => {
    const segs = this.statusBreakdown();
    const total = this.stats()?.totalProperties ?? 0;
    if (total === 0) return 'conic-gradient(#e9ecef 0 100%)';
    let acc = 0;
    const stops = segs
      .filter((s) => s.count > 0)
      .map((s) => {
        const start = (acc / total) * 100;
        acc += s.count;
        const end = (acc / total) * 100;
        return `${s.color} ${start}% ${end}%`;
      });
    return `conic-gradient(${stops.join(', ')})`;
  });

  loading = computed(() => !this.company() || !this.stats());

  iconFor(item: ActivityItem): string {
    return ACTIVITY_ICONS[item.type] ?? 'fa-circle-info text-secondary';
  }
}
