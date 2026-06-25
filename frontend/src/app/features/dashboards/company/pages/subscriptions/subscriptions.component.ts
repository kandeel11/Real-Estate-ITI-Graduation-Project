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
import { BILLING_MONTHLY, BILLING_ANNUAL } from '../../../../../core/models/payment.model';
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

  // ── Upgrade state ───────────────────────────────────────────────
  plans = signal<SubscriptionPlan[]>([]);
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
    this.handleCheckoutReturn();
  }

  private load(): void {
    this.subSvc.getActive().subscribe((sub) => this.subscription.set(sub));
    this.subSvc.getUsage().subscribe((usage) => this.usage.set(usage));
    this.subSvc.getPayments().subscribe((payments) => this.invoices.set(payments));
    this.subSvc.getPlans().subscribe((plans) => this.plans.set(plans));
  }

  /**
   * After returning from Stripe with `?checkout=success&session_id=...`,
   * confirm the payment server-side (finalizes the upgrade without needing the
   * webhook), then refresh. Idempotent — safe even if the webhook already ran.
   */
  private handleCheckoutReturn(): void {
    const params = new URLSearchParams(window.location.search);
    const checkout = params.get('checkout');
    const sessionId = params.get('session_id');
    this.stripUrlParams();

    if (checkout !== 'success' || !sessionId) return;

    this.checkingOut.set(true);
    this.paymentService.confirmSubscription(sessionId).subscribe({
      next: () => {
        this.checkingOut.set(false);
        this.planSuccess.set('Payment confirmed — your plan has been upgraded.');
        setTimeout(() => this.planSuccess.set(null), 5000);
        // Activation runs through the outbox (near-instant); reload now and
        // once more shortly after to catch it.
        this.load();
        setTimeout(() => this.load(), 1500);
      },
      error: (err) => {
        this.checkingOut.set(false);
        this.planError.set(err?.error?.error ?? 'Could not confirm your payment. Please refresh.');
      },
    });
  }

  /** Remove checkout query params so a refresh doesn't re-trigger confirmation. */
  private stripUrlParams(): void {
    window.history.replaceState({}, '', window.location.pathname);
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

  /**
   * Upgrade to the pressed card's plan (no plan picker). Paid plans run the
   * full payment flow — SelectPlan (creates a PendingPayment subscription for
   * this plan) → Stripe checkout → the webhook activates it. Free downgrades
   * skip payment via the direct upgrade endpoint.
   */
  upgradeTo(plan: SubscriptionPlan | null): void {
    if (!plan || this.planActionBusy() || this.checkingOut()) return;
    this.planError.set(null);

    if (plan.priceMonthly === 0) {
      this.planActionBusy.set(true);
      this.subSvc.upgrade(plan.id).subscribe({
        next: () => {
          this.planActionBusy.set(false);
          this.planSuccess.set('Plan changed successfully.');
          setTimeout(() => this.planSuccess.set(null), 4000);
          this.load();
        },
        error: (err) => {
          this.planActionBusy.set(false);
          this.planError.set(err?.error?.error ?? 'Failed to change the plan.');
        },
      });
      return;
    }

    this.checkingOut.set(true);
    const origin = window.location.origin;
    this.paymentService.startSubscriptionCheckout({
      userId: this.auth.user()?.id ?? '',
      companyId: this.auth.user()?.companyId ?? null,
      planId: plan.id,
      billingCycle: this.billingCycle() === 'monthly' ? BILLING_MONTHLY : BILLING_ANNUAL,
      successRedirectUrl: `${origin}/dashboard/company/subscriptions?checkout=success`,
      cancelRedirectUrl:  `${origin}/dashboard/company/subscriptions?checkout=cancel`,
    }).subscribe({
      next: (res) => { window.location.href = res.checkoutUrl; },
      error: (err) => {
        this.checkingOut.set(false);
        this.planError.set(err?.error?.error ?? err?.error?.message ?? 'Checkout failed. Please try again.');
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
