/**
 * Company Subscription Page
 * Live plan + usage + payment history from /api/subscriptions/mine*.
 * Manage Plan switches plans via POST mine/upgrade (charging is Phase 2);
 * cancel via POST mine/cancel.
 */

import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { DatePipe, CurrencyPipe } from '@angular/common';
import { CompanySubscriptionService } from '../../services/company-subscription.service';
import { UsageMeterComponent } from '../../../../shared/components/usage-meter/usage-meter.component';
import { ConfirmModalComponent } from '../../../../shared/components/modal/confirm-modal.component';
import {
  SubscriptionInfo,
  SubscriptionUsage,
  PaymentItem,
  SubscriptionPlan,
} from '../../../../shared/models/company.model';

@Component({
  selector: 'app-company-subscriptions',
  standalone: true,
  imports: [DatePipe, CurrencyPipe, UsageMeterComponent, ConfirmModalComponent],
  templateUrl: './subscriptions.component.html',
  styleUrl: './subscriptions.component.css',
})
export class SubscriptionsComponent implements OnInit {
  private subSvc = inject(CompanySubscriptionService);

  subscription = signal<SubscriptionInfo | null | undefined>(undefined);
  usage = signal<SubscriptionUsage | null>(null);
  invoices = signal<PaymentItem[]>([]);

  // undefined = still loading; null = loaded, no active subscription
  loading = computed(() => this.subscription() === undefined);

  features = computed(() => {
    const sub = this.subscription();
    if (!sub) return [];
    // AI price estimation was cut from the product — not shown even though
    // the backend still returns the flag
    return [
      { label: 'Advanced analytics', on: !!sub.hasAdvancedAnalytics },
      { label: 'Featured listings', on: !!sub.hasFeaturedListings },
      { label: 'AI chatbot', on: !!sub.hasAiChatbot },
      { label: 'AI lead prioritization', on: !!sub.hasAiLeadPrioritization },
      { label: 'AI description generator', on: !!sub.hasAiDescriptionGenerator },
    ];
  });

  /** Cancel only makes sense on paid tiers — it downgrades back to Free. */
  isPremium = computed(() =>
    (this.subscription()?.tier ?? '').startsWith('Premium')
  );

  // ── Upgrade Plan modal ──────────────────────────────────────────
  showPlanModal = signal(false);
  plans = signal<SubscriptionPlan[]>([]);
  selectedPlanId = signal<string>('');
  planActionBusy = signal(false);
  planError = signal<string | null>(null);
  planSuccess = signal<string | null>(null);

  /** Company-audience plans only (they're the ones with broker seats). */
  companyPlans = computed(() =>
    this.plans().filter((p) => p.maxBrokers > 0 && p.isActive)
  );

  // ── Cancel confirmation ─────────────────────────────────────────
  showCancelConfirm = signal(false);
  cancelling = signal(false);

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.subSvc.getActive().subscribe((sub) => this.subscription.set(sub));
    this.subSvc.getUsage().subscribe((usage) => this.usage.set(usage));
    this.subSvc.getPayments().subscribe((payments) => this.invoices.set(payments));
  }

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

  openPlanModal(): void {
    this.planError.set(null);
    this.selectedPlanId.set('');
    this.showPlanModal.set(true);
    if (this.plans().length === 0) {
      this.subSvc.getPlans().subscribe((plans) => this.plans.set(plans));
    }
  }

  onPlanSelected(event: Event): void {
    this.selectedPlanId.set((event.target as HTMLSelectElement).value);
  }

  confirmUpgrade(): void {
    const planId = this.selectedPlanId();
    if (!planId || this.planActionBusy()) return;

    this.planActionBusy.set(true);
    this.planError.set(null);

    this.subSvc.upgrade(planId).subscribe({
      next: () => {
        this.planActionBusy.set(false);
        this.showPlanModal.set(false);
        this.planSuccess.set('Plan upgraded successfully.');
        setTimeout(() => this.planSuccess.set(null), 4000);
        this.load();
      },
      error: (err) => {
        this.planActionBusy.set(false);
        this.planError.set(err?.error?.error ?? 'Failed to change the plan.');
      },
    });
  }

  confirmCancel(): void {
    if (this.cancelling()) return;
    this.cancelling.set(true);

    this.subSvc.cancel('Cancelled from the dashboard').subscribe({
      next: () => {
        this.cancelling.set(false);
        this.showCancelConfirm.set(false);
        this.planSuccess.set('Subscription cancelled — you are back on the Free plan.');
        setTimeout(() => this.planSuccess.set(null), 5000);
        this.load();
      },
      error: (err) => {
        this.cancelling.set(false);
        this.showCancelConfirm.set(false);
        this.planError.set(err?.error?.error ?? 'Failed to cancel the subscription.');
      },
    });
  }
}
