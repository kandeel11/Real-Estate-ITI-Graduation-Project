import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { finalize } from 'rxjs';
import { AuthService } from '../../../../../core/services/auth.service';
import { PaymentService } from '../../../../../core/services/payment.service';
import { OwnerSubscriptionService, BrokerPlan, BrokerActiveSubscription } from '../../services/owner-subscription.service';

@Component({
  selector: 'app-owner-subscription',
  standalone: true,
  imports: [],
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

  // Individual plans: tier 0 (Free Individual) and tier 1 (Premium Individual)
  readonly freePlan = computed(() => this.plans().find(p => p.tier === 0) ?? null);
  readonly premiumPlan = computed(() => this.plans().find(p => p.tier === 1) ?? null);
  readonly currentPlanName = computed(() => this.active()?.planName ?? '');

  ngOnInit(): void {
    this.load();
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

    const userId = this.auth.user()?.id ?? '';
    const origin = window.location.origin;
    const base = '/dashboard/owner/subscription';

    this.paymentService.checkoutSubscription({
      userId,
      subscriptionId: this.active()?.id ?? null,
      planId: plan.id,
      billingCycle: this.billingCycle() === 'monthly' ? 0 : 1,
      successRedirectUrl: `${origin}${base}?checkout=success`,
      cancelRedirectUrl:  `${origin}${base}?checkout=cancel`,
    }).subscribe({
      next: (res) => {
        window.location.href = res.checkoutUrl;
      },
      error: (err) => {
        this.checkingOut.set(false);
        this.checkoutError.set(err?.error?.message ?? 'Checkout failed. Please try again.');
      },
    });
  }

  readonly skeletonRows = [1, 2, 3, 4];
}
