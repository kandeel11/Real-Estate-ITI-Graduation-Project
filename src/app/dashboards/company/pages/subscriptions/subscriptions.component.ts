/**
 * Company Subscription Page
 * Live plan + usage + payment history from /api/subscriptions/mine[/usage|/payments].
 * Online payment (upgrades/charging) is still Phase 2 — only history is real.
 */

import { Component, inject, signal, computed } from '@angular/core';
import { DatePipe, CurrencyPipe } from '@angular/common';
import { toSignal } from '@angular/core/rxjs-interop';
import { CompanySubscriptionService } from '../../services/company-subscription.service';
import { UsageMeterComponent } from '../../../../shared/components/usage-meter/usage-meter.component';
import { PaymentItem } from '../../../../shared/models/company.model';

@Component({
  selector: 'app-company-subscriptions',
  standalone: true,
  imports: [DatePipe, CurrencyPipe, UsageMeterComponent],
  templateUrl: './subscriptions.component.html',
  styleUrl: './subscriptions.component.css',
})
export class SubscriptionsComponent {
  private subSvc = inject(CompanySubscriptionService);

  subscription = toSignal(this.subSvc.getActive());
  usage = toSignal(this.subSvc.getUsage());

  // undefined = still loading; null = loaded, no active subscription
  loading = computed(() => this.subscription() === undefined);

  upgradeNotice = signal(false);

  features = computed(() => {
    const sub = this.subscription();
    if (!sub) return [];
    return [
      { label: 'Advanced analytics', on: !!sub.hasAdvancedAnalytics },
      { label: 'Featured listings', on: !!sub.hasFeaturedListings },
      { label: 'AI price estimation', on: !!sub.hasAiPriceEstimation },
      { label: 'AI chatbot', on: !!sub.hasAiChatbot },
      { label: 'AI lead prioritization', on: !!sub.hasAiLeadPrioritization },
      { label: 'AI description generator', on: !!sub.hasAiDescriptionGenerator },
    ];
  });

  invoices = toSignal(this.subSvc.getPayments(), {
    initialValue: [] as PaymentItem[],
  });

  invoiceBadge(status: string): string {
    switch (status) {
      case 'Paid':
        return 'badge bg-success';
      case 'Pending':
        return 'badge bg-warning text-dark';
      case 'Failed':
        return 'badge bg-danger';
      default:
        return 'badge bg-secondary';
    }
  }

  onUpgrade(): void {
    this.upgradeNotice.set(true);
    setTimeout(() => this.upgradeNotice.set(false), 4000);
  }
}
