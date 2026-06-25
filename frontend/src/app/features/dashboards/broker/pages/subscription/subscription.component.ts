import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { finalize } from 'rxjs';
import { AuthService } from '../../../../../core/services/auth.service';
import { PaymentService } from '../../../../../core/services/payment.service';
import { BILLING_MONTHLY, BILLING_ANNUAL } from '../../../../../core/models/payment.model';
import { UsageMeterComponent } from '../../../../../shared/components/usage-meter/usage-meter.component';
import { BrokerSubscriptionService, BrokerPlan, BrokerActiveSubscription, BrokerUsage } from '../../services/broker-subscription.service';

@Component({
  selector: 'app-broker-subscription',
  standalone: true,
  imports: [UsageMeterComponent],
  templateUrl: './subscription.component.html',
  styleUrl: './subscription.component.css',
})
export class BrokerSubscriptionComponent implements OnInit {
  private readonly svc = inject(BrokerSubscriptionService);
  private readonly auth = inject(AuthService);
  private readonly paymentService = inject(PaymentService);

  readonly isCompanyBroker = computed(() => !!this.auth.user()?.companyId);

  loading = signal(true);
  error = signal<string | null>(null);
  plans = signal<BrokerPlan[]>([]);
  active = signal<BrokerActiveSubscription | null>(null);
  usage = signal<BrokerUsage | null>(null);
  billingCycle = signal<'monthly' | 'annual'>('monthly');
  priceOpacity = signal(1);
  checkingOut = signal(false);
  checkoutError = signal<string | null>(null);
  checkoutSuccess = signal<string | null>(null);

  /**
   * Features the company broker inherits from the company subscription.
   * `/subscriptions/mine` resolves to the company plan for company brokers,
   * so `active()` already carries the company's flags and limits.
   */
  readonly inheritedFeatures = computed(() => {
    const s = this.active();
    if (!s) return [];
    return [
      { label: 'AI Chatbot', on: !!s.hasAiChatbot },
      { label: 'AI Description Generator', on: !!s.hasAiDescriptionGenerator },
      { label: 'Advanced Analytics', on: !!s.hasAdvancedAnalytics },
      { label: 'Featured Listings', on: !!s.hasFeaturedListings },
      { label: 'AI Lead Prioritization', on: !!s.hasAiLeadPrioritization },
    ];
  });

  // Individual plans: tier 0 (Free Individual) and tier 1 (Premium Individual)
  readonly freePlan = computed(() =>
    this.plans().find(p => p.tier === 0) ?? null
  );

  readonly premiumPlan = computed(() =>
    this.plans().find(p => p.tier === 1) ?? null
  );

  readonly currentPlanName = computed(() => this.active()?.planName ?? '');

  ngOnInit(): void {
    this.load();
    this.handleCheckoutReturn();
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
    window.history.replaceState({}, '', window.location.pathname);

    if (checkout !== 'success' || !sessionId) return;

    this.checkingOut.set(true);
    this.paymentService.confirmSubscription(sessionId).subscribe({
      next: () => {
        this.checkingOut.set(false);
        this.checkoutSuccess.set('Payment confirmed — your plan has been upgraded.');
        setTimeout(() => this.checkoutSuccess.set(null), 5000);
        this.load();
        setTimeout(() => this.load(), 1500);
      },
      error: (err) => {
        this.checkingOut.set(false);
        this.checkoutError.set(err?.error?.error ?? 'Could not confirm your payment. Please refresh.');
      },
    });
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    // Company brokers also load shared usage; independent brokers don't need it.
    const total = this.isCompanyBroker() ? 3 : 2;
    let done = 0;
    const finish = () => { if (++done === total) this.loading.set(false); };

    this.svc.getPlans().pipe(finalize(finish)).subscribe({
      next: plans => {
        // Keep only Individual-audience plans (tiers 0 and 1)
        const individual = plans.filter(p => p.tier === 0 || p.tier === 1);
        this.plans.set(individual.length > 0 ? individual : plans.slice(0, 2));
      },
      error: () => { this.error.set('Failed to load plans.'); finish(); },
    });

    this.svc.getActive().pipe(finalize(finish)).subscribe({
      next: sub => this.active.set(sub),
      error: () => finish(),
    });

    if (this.isCompanyBroker()) {
      this.svc.getUsage().pipe(finalize(finish)).subscribe({
        next: u => this.usage.set(u),
        error: () => finish(),
      });
    }
  }

  setBilling(cycle: 'monthly' | 'annual'): void {
    if (this.billingCycle() === cycle) return;
    this.priceOpacity.set(0);
    setTimeout(() => { this.billingCycle.set(cycle); this.priceOpacity.set(1); }, 150);
  }

  formatValue(value: number): string {
    return value >= 999999 ? 'Unlimited' : value.toLocaleString();
  }

  annualMonthly(plan: BrokerPlan): string {
    return (plan.priceAnnual / 12).toFixed(2);
  }

  isCurrent(plan: BrokerPlan): boolean {
    return this.currentPlanName() === plan.name;
  }

  /**
   * Upgrade to the pressed card's plan (the individual page only offers
   * Premium Individual). Runs the full payment flow — SelectPlan creates a
   * PendingPayment subscription for this plan, then Stripe checkout; the
   * webhook activates it on success.
   */
  upgradeTo(plan: BrokerPlan): void {
    if (this.checkingOut() || plan.priceMonthly === 0) return;
    this.checkingOut.set(true);
    this.checkoutError.set(null);

    const origin = window.location.origin;
    const base = '/dashboard/broker/subscription';
    this.paymentService.startSubscriptionCheckout({
      userId: this.auth.user()?.id ?? '',
      companyId: this.auth.user()?.companyId ?? null,
      planId: plan.id,
      billingCycle: this.billingCycle() === 'monthly' ? BILLING_MONTHLY : BILLING_ANNUAL,
      successRedirectUrl: `${origin}${base}?checkout=success`,
      cancelRedirectUrl:  `${origin}${base}?checkout=cancel`,
    }).subscribe({
      next: (res) => { window.location.href = res.checkoutUrl; },
      error: (err) => {
        this.checkingOut.set(false);
        this.checkoutError.set(err?.error?.error ?? err?.error?.message ?? 'Checkout failed. Please try again.');
      },
    });
  }

  readonly skeletonRows = [1, 2, 3, 4];
}
