/**
 * Company Subscription Page
 * Live plan + usage + payment history from /api/subscriptions/mine*.
 * Manage Plan switches plans via POST mine/upgrade (charging is Phase 2);
 * cancel via POST mine/cancel.
 */

import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { DatePipe, CurrencyPipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { CompanySubscriptionService } from '../../services/company-subscription.service';
import { PaymentService } from '../../../../../core/services/payment.service';
import { AuthService } from '../../../../../core/services/auth.service';
import { UsageMeterComponent } from '../../../../../shared/components/usage-meter/usage-meter.component';
import { ConfirmModalComponent } from '../../../../../shared/components/modal/confirm-modal.component';
import {
  SubscriptionInfo,
  SubscriptionUsage,
  PaymentItem,
  SubscriptionPlan,
} from '../../../../../shared/models/company.model';

@Component({
  selector: 'app-company-subscriptions',
  standalone: true,
  imports: [DatePipe, CurrencyPipe, UsageMeterComponent, ConfirmModalComponent, TranslatePipe],
  templateUrl: './subscriptions.component.html',
  styleUrl: './subscriptions.component.css',
})
export class SubscriptionsComponent implements OnInit {
  private subSvc = inject(CompanySubscriptionService);
  private paymentService = inject(PaymentService);
  private auth = inject(AuthService);

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
      { labelKey: 'SUBSCRIPTIONS.FEAT_ANALYTICS', on: !!sub.hasAdvancedAnalytics },
      { labelKey: 'SUBSCRIPTIONS.FEAT_FEATURED', on: !!sub.hasFeaturedListings },
      { labelKey: 'SUBSCRIPTIONS.FEAT_CHATBOT', on: !!sub.hasAiChatbot },
      { labelKey: 'SUBSCRIPTIONS.FEAT_LEAD', on: !!sub.hasAiLeadPrioritization },
      { labelKey: 'SUBSCRIPTIONS.FEAT_DESC_GEN', on: !!sub.hasAiDescriptionGenerator },
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
  checkingOut = signal(false);

  /** Company-audience plans: tier 2 (Free Company) and tier 3 (Premium Company). */
  companyPlans = computed(() =>
    this.plans().filter(p => (p.tier === 2 || p.tier === 3) && p.isActive)
  );

  readonly freePlan = computed(() =>
    this.companyPlans().find(p => p.tier === 2) ?? null
  );

  readonly premiumPlan = computed(() =>
    this.companyPlans().find(p => p.tier === 3) ?? null
  );

  billingCycle = signal<'monthly' | 'annual'>('monthly');
  priceOpacity = signal(1);

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
    this.subSvc.getPlans().subscribe((plans) => this.plans.set(plans));
  }

  setBilling(cycle: 'monthly' | 'annual'): void {
    if (this.billingCycle() === cycle) return;
    this.priceOpacity.set(0);
    setTimeout(() => { this.billingCycle.set(cycle); this.priceOpacity.set(1); }, 150);
  }

  formatValue(value: number): string {
    return value >= 999999 ? 'Unlimited' : value.toLocaleString();
  }

  annualMonthly(plan: SubscriptionPlan): string {
    return (plan.priceAnnual / 12).toFixed(2);
  }

  isCurrent(plan: SubscriptionPlan): boolean {
    return (this.subscription()?.planName ?? '') === plan.name;
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
    const plan = this.plans().find(p => p.id === planId);
    if (!planId || !plan || this.planActionBusy()) return;

    // Free plan downgrades use the direct upgrade endpoint; paid plans go through Stripe checkout
    if (plan.priceMonthly === 0) {
      this.planActionBusy.set(true);
      this.planError.set(null);
      this.subSvc.upgrade(planId).subscribe({
        next: () => {
          this.planActionBusy.set(false);
          this.showPlanModal.set(false);
          this.planSuccess.set('Plan changed successfully.');
          setTimeout(() => this.planSuccess.set(null), 4000);
          this.load();
        },
        error: (err) => {
          this.planActionBusy.set(false);
          this.planError.set(err?.error?.error ?? 'Failed to change the plan.');
        },
      });
    } else {
      this.checkingOut.set(true);
      this.planError.set(null);
      const userId = this.auth.user()?.id ?? '';
      const origin = window.location.origin;

      this.paymentService.checkoutSubscription({
        userId,
        subscriptionId: this.subscription()?.id ?? null,
        planId: plan.id,
        billingCycle: this.billingCycle() === 'monthly' ? 0 : 1,
        successRedirectUrl: `${origin}/dashboard/company/subscriptions?checkout=success`,
        cancelRedirectUrl:  `${origin}/dashboard/company/subscriptions?checkout=cancel`,
      }).subscribe({
        next: (res) => {
          window.location.href = res.checkoutUrl;
        },
        error: (err) => {
          this.checkingOut.set(false);
          this.planError.set(err?.error?.message ?? 'Checkout failed. Please try again.');
        },
      });
    }
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
