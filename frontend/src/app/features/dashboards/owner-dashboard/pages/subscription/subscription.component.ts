import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { finalize } from 'rxjs';
import { AuthService } from '../../../../../core/services/auth.service';
import { PaymentService } from '../../../../../core/services/payment.service';
import { BILLING_MONTHLY, BILLING_ANNUAL } from '../../../../../core/models/payment.model';
import { ConfirmModalComponent } from '../../../../../shared/components/modal/confirm-modal.component';
import { OwnerSubscriptionService, BrokerPlan, BrokerActiveSubscription } from '../../services/owner-subscription.service';

@Component({
  selector: 'app-owner-subscription',
  standalone: true,
  imports: [ConfirmModalComponent],
  templateUrl: './subscription.component.html',
  styleUrl: './subscription.component.css',
})
export class OwnerSubscriptionComponent implements OnInit {
  private readonly svc = inject(OwnerSubscriptionService);
  private readonly auth = inject(AuthService);
  private readonly paymentService = inject(PaymentService);

  readonly isCompanyBroker = computed(() => !!this.auth.user()?.companyId);

  loading = signal(true);
  error = signal<string | null>(null);
  plans = signal<BrokerPlan[]>([]);
  active = signal<BrokerActiveSubscription | null>(null);
  billingCycle = signal<'monthly' | 'annual'>('monthly');
  priceOpacity = signal(1);
  checkingOut = signal(false);
  checkoutError = signal<string | null>(null);

  // ── Cancellation (premium → Free) ───────────────────────────────
  showCancelConfirm = signal(false);
  cancelling = signal(false);

  // Individual plans: tier 0 (Free Individual) and tier 1 (Premium Individual)
  readonly freePlan = computed(() => this.plans().find(p => p.tier === 0) ?? null);
  readonly premiumPlan = computed(() => this.plans().find(p => p.tier === 1) ?? null);
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
    let done = 0;
    const finish = () => { if (++done === 2) this.loading.set(false); };

    this.svc.getPlans().pipe(finalize(finish)).subscribe({
      next: plans => {
        const individual = plans.filter(p => p.tier === 0 || p.tier === 1);
        this.plans.set(individual.length > 0 ? individual : plans.slice(0, 2));
      },
      error: () => { this.error.set('Failed to load plans.'); finish(); },
    });

    this.svc.getActive().pipe(finalize(finish)).subscribe({
      next: sub => this.active.set(sub),
      error: () => finish(),
    });
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

  checkout(plan: BrokerPlan): void {
    if (this.checkingOut() || plan.priceMonthly === 0) return;
    this.checkingOut.set(true);
    this.checkoutError.set(null);

    const origin = window.location.origin;
    const base = '/dashboard/owner/subscription';

    // Full flow: SelectPlan creates a PendingPayment subscription for the chosen
    // plan, then Stripe checkout. Without SelectPlan the success webhook/confirm
    // has no PendingPayment row to activate, so the plan never changes.
    this.paymentService.startSubscriptionCheckout({
      userId: this.auth.user()?.id ?? '',
      companyId: this.auth.user()?.companyId ?? null,
      planId: plan.id,
      billingCycle: this.billingCycle() === 'monthly' ? BILLING_MONTHLY : BILLING_ANNUAL,
      successRedirectUrl: `${origin}${base}?checkout=success`,
      cancelRedirectUrl:  `${origin}${base}?checkout=cancel`,
    }).subscribe({
      next: (res) => {
        window.location.href = res.checkoutUrl;
      },
      error: (err) => {
        this.checkingOut.set(false);
        this.checkoutError.set(err?.error?.error ?? err?.error?.message ?? 'Checkout failed. Please try again.');
      },
    });
  }

  // ── Cancellation ────────────────────────────────────────────────

  openCancel(): void {
    this.checkoutError.set(null);
    this.showCancelConfirm.set(true);
  }

  closeCancel(): void {
    if (!this.cancelling()) this.showCancelConfirm.set(false);
  }

  /** Cancel the premium subscription — the backend drops the owner to Free. */
  confirmCancel(): void {
    if (this.cancelling()) return;
    this.cancelling.set(true);
    this.svc.cancel('Cancelled from the dashboard').subscribe({
      next: () => {
        this.cancelling.set(false);
        this.showCancelConfirm.set(false);
        this.load();
      },
      error: (err) => {
        this.cancelling.set(false);
        this.showCancelConfirm.set(false);
        this.checkoutError.set(err?.error?.error ?? 'Failed to cancel the subscription.');
      },
    });
  }

  readonly skeletonRows = [1, 2, 3, 4];
}
