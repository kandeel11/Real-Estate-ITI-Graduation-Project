import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { finalize } from 'rxjs';
import { SubscriptionService } from '../../services/subscription.service';
import { SubscriptionPlan } from '../../models/subscription.models';

@Component({
  selector: 'app-subscription-plans',
  standalone: true,
  imports: [],
  templateUrl: './subscription-plans.component.html',
  styleUrl: './subscription-plans.component.css',
})
export class SubscriptionPlansComponent implements OnInit {
  private readonly subService = inject(SubscriptionService);

  protected loading = signal(true);
  protected error = signal<string | null>(null);
  protected plans = signal<SubscriptionPlan[]>([]);
  protected billingCycle = signal<'monthly' | 'annual'>('monthly');
  protected priceOpacity = signal(1);
  protected showModal = signal(false);

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

  openModal(): void {
    this.showModal.set(true);
  }

  closeModal(): void {
    this.showModal.set(false);
  }

  readonly skeletonRows = [1, 2, 3, 4, 5, 6, 7, 8];
}
