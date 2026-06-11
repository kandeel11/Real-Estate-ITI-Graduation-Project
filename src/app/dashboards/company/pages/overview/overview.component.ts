/**
 * Company Dashboard Overview Page
 * Stat cards, subscription usage meters, top brokers, and activity feed.
 */

import { Component, inject, computed } from '@angular/core';
import { DecimalPipe, DatePipe } from '@angular/common';
import { toSignal } from '@angular/core/rxjs-interop';
import { CompanyService } from '../../services/company.service';
import { CompanySubscriptionService } from '../../services/company-subscription.service';
import { UsageMeterComponent } from '../../../../shared/components/usage-meter/usage-meter.component';
import { Broker } from '../../../../shared/models/company.model';

interface ActivityItem {
  icon: string;
  text: string;
  when: string;
}

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

  topBrokers = computed(() =>
    [...this.brokers()]
      .filter((b) => b.isActive)
      .sort((a, b) => (b.closedDealsCount ?? 0) - (a.closedDealsCount ?? 0))
      .slice(0, 3)
  );

  loading = computed(() => !this.company() || !this.stats());

  // Mocked until a real activity feed endpoint exists
  activity: ActivityItem[] = [
    { icon: 'fa-circle-check text-success', text: 'Property "Maadi 3BR" approved', when: '2h ago' },
    { icon: 'fa-envelope text-primary', text: 'New inquiry assigned to Sara Hassan', when: '4h ago' },
    { icon: 'fa-handshake text-success', text: 'Deal closed by Mahmoud Ali', when: '1d ago' },
    { icon: 'fa-user-plus text-secondary', text: 'Broker Layla Ibrahim joined the team', when: '3d ago' },
  ];
}
