/**
 * Company Dashboard Overview Page
 * Stat cards, subscription usage meters, top brokers, and activity feed —
 * all from live endpoints (/companies/mine/*, /subscriptions/mine*).
 */

import { Component, inject, computed } from '@angular/core';
import { DecimalPipe, DatePipe } from '@angular/common';
import { toSignal } from '@angular/core/rxjs-interop';
import { CompanyService } from '../../services/company.service';
import { CompanySubscriptionService } from '../../services/company-subscription.service';
import { UsageMeterComponent } from '../../../../shared/components/usage-meter/usage-meter.component';
import { Broker, ActivityItem } from '../../../../shared/models/company.model';

const ACTIVITY_ICONS: Record<string, string> = {
  listing_created: 'fa-house text-primary',
  broker_joined: 'fa-user-plus text-secondary',
  inquiry_received: 'fa-envelope text-primary',
};

@Component({
  selector: 'app-company-overview',
  standalone: true,
  imports: [DecimalPipe, DatePipe, UsageMeterComponent],
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

  loading = computed(() => !this.company() || !this.stats());

  iconFor(item: ActivityItem): string {
    return ACTIVITY_ICONS[item.type] ?? 'fa-circle-info text-secondary';
  }
}
