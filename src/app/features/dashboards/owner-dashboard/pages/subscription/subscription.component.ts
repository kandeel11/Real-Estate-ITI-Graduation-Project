import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../../../../core/services/auth.service';
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
  private readonly router = inject(Router);

  readonly isCompanyBroker = computed(() => !!this.auth.user()?.companyId);

  loading = signal(true);
  error = signal<string | null>(null);
  plans = signal<BrokerPlan[]>([]);
  active = signal<BrokerActiveSubscription | null>(null);
  billingCycle = signal<'monthly' | 'annual'>('monthly');
  priceOpacity = signal(1);
  showModal = signal(false);
  upgrading = signal(false);
  upgradeError = signal<string | null>(null);
  upgradeSuccess = signal<string | null>(null);

  private tierStr(p: BrokerPlan): string {
    return String(p.tier ?? '');
  }

  readonly freePlan = computed(() =>
    this.plans().find(p => this.tierStr(p) === 'FreeIndividual') ??
    this.plans().find(p => p.priceMonthly === 0 && (this.tierStr(p).includes('Individual') || p.maxProperties <= 5)) ??
    null
  );

  readonly premiumPlan = computed(() =>
    this.plans().find(p => this.tierStr(p) === 'PremiumIndividual') ??
    this.plans().find(p => p.priceMonthly > 0 && (this.tierStr(p).includes('Individual') || p.maxProperties <= 30)) ??
    null
  );

  readonly currentTier = computed(() => this.active()?.tier ?? '');

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
        const individual = plans.filter(p => {
          const tier = String(p.tier ?? '');
          return tier.includes('Individual') || (!tier.includes('Company') && !tier.includes('Tenant'));
        });
        this.plans.set(individual.length > 0 ? individual : plans.filter(p => p.maxProperties <= 30));
      },
      error: () => { this.error.set('Failed to load plans.'); finish(); },
    });

    this.svc.getActive().pipe(finalize(finish)).subscribe({
      next: sub => this.active.set(sub),
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
    return this.currentTier() === String(plan.tier ?? '');
  }

  openModal(): void {
    this.upgradeError.set(null);
    this.showModal.set(true);
  }

  closeModal(): void {
    this.showModal.set(false);
  }

  goToCompany(): void {
    this.router.navigate(['/dashboard/company/subscriptions']);
  }

  readonly skeletonRows = [1, 2, 3, 4];
}
