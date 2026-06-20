import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { finalize } from 'rxjs';
import { SubscriptionService } from '../../services/subscription.service';
import { SubscriptionPlan, ActiveSubscription } from '../../models/subscription.models';
import { PaymentService } from '../../../../core/services/payment.service';
import { AuthService } from '../../../../core/services/auth.service';
import { BILLING_MONTHLY, BILLING_ANNUAL } from '../../../../core/models/payment.model';

@Component({
  selector: 'app-subscription-plans',
  standalone: true,
  imports: [],
  templateUrl: './subscription-plans.component.html',
  styleUrl: './subscription-plans.component.css',
})
export class SubscriptionPlansComponent implements OnInit {
  private readonly subService = inject(SubscriptionService);
  private readonly paymentService = inject(PaymentService);
  private readonly auth = inject(AuthService);

  protected loading = signal(true);
  protected error = signal<string | null>(null);
  protected plans = signal<SubscriptionPlan[]>([]);
  protected active = signal<ActiveSubscription | null>(null);
  protected billingCycle = signal<'monthly' | 'annual'>('monthly');
  protected priceOpacity = signal(1);
  protected checkingOut = signal(false);
  protected checkoutError = signal<string | null>(null);
  protected checkoutSuccess = signal<string | null>(null);

  /** True when the given plan is the caller's current active plan. */
  protected isCurrent(plan: SubscriptionPlan): boolean {
    return this.active()?.tier === plan.tier;
  }

  protected readonly freePlan = computed(() =>
    this.plans().find(p => p.tier === 'FreeTenant') ??
    this.plans().find(p => p.priceMonthly === 0) ??
    this.plans()[0] ??
    null
  );

  protected readonly premiumPlan = computed(() =>
    this.plans().find(p => p.tier === 'PremiumTenant') ??
    this.plans().find(p => p.priceMonthly > 0) ??
    this.plans()[1] ??
    null
  );

  ngOnInit(): void {
    this.loadPlans();
    this.handleCheckoutReturn();
  }

  /**
   * Upgrade to the pressed card's plan via the full payment flow — SelectPlan
   * creates a PendingPayment subscription, then Stripe checkout. A tenant is an
   * individual user, so no companyId.
   */
  upgradeTo(plan: SubscriptionPlan): void {
    if (this.checkingOut() || plan.priceMonthly === 0) return;
    this.checkingOut.set(true);
    this.checkoutError.set(null);
    this.checkoutSuccess.set(null);

    const origin = window.location.origin;
    const path = window.location.pathname;
    this.paymentService.startSubscriptionCheckout({
      userId: this.auth.user()?.id ?? '',
      companyId: null,
      planId: plan.id,
      billingCycle: this.billingCycle() === 'monthly' ? BILLING_MONTHLY : BILLING_ANNUAL,
      successRedirectUrl: `${origin}${path}?checkout=success`,
      cancelRedirectUrl:  `${origin}${path}?checkout=cancel`,
    }).subscribe({
      next: (res) => { window.location.href = res.checkoutUrl; },
      error: (err) => {
        this.checkingOut.set(false);
        this.checkoutError.set(err?.error?.error ?? err?.error?.message ?? 'Checkout failed. Please try again.');
      },
    });
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
        this.loadPlans();
      },
      error: (err) => {
        this.checkingOut.set(false);
        this.checkoutError.set(err?.error?.error ?? 'Could not confirm your payment. Please refresh.');
      },
    });
  }

  loadPlans(): void {
    this.loading.set(true);
    this.error.set(null);
    this.subService
      .getActivePlans()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: plans => this.plans.set(plans),
        error: () => this.error.set('Failed to load plans. Please try again.'),
      });
    this.subService.getMine().subscribe(sub => this.active.set(sub));
  }

  setBilling(cycle: 'monthly' | 'annual'): void {
    if (this.billingCycle() === cycle) return;
    this.priceOpacity.set(0);
    setTimeout(() => {
      this.billingCycle.set(cycle);
      this.priceOpacity.set(1);
    }, 150);
  }

  formatValue(value: number): string {
    return value >= 999999 ? 'Unlimited' : value.toLocaleString();
  }

  annualMonthly(plan: SubscriptionPlan): string {
    return (plan.priceAnnual / 12).toFixed(2);
  }

  readonly skeletonRows = [1, 2, 3, 4, 5, 6, 7, 8];
}
