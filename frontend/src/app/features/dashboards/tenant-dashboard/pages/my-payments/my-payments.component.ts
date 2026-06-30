import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PaymentService } from '../../../../../core/services/payment.service';
import { RentPayment, PaymentSchedule, PaymentPeriod, PaymentStatus } from '../../../../../core/models/payment.model';

type ActiveTab = 'schedule' | 'history';

@Component({
  selector: 'app-my-payments',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './my-payments.component.html',
  styleUrls: ['./my-payments.component.scss'],
})
export class MyPaymentsComponent implements OnInit {
  private paymentService = inject(PaymentService);

  activeTab = signal<ActiveTab>('schedule');

  // Schedule tab
  scheduleLoading = signal(false);
  scheduleError = signal<string | null>(null);
  schedule = signal<PaymentSchedule | null>(null);
  showAll = signal(false);

  // History tab
  historyLoading = signal(false);
  historyError = signal<string | null>(null);
  payments = signal<RentPayment[]>([]);

  // dueDate → payment status map (used to override date-based schedule labels)
  private readonly paymentByDueDate = computed(() => {
    const map = new Map<string, PaymentStatus>();
    this.payments().forEach(p => { if (p.dueDate) map.set(p.dueDate, p.status); });
    return map;
  });

  // Recompute isCurrent/isNext based on actual payment statuses, not just dates.
  // A period whose payment is Paid/PaidExternally is treated as "Past".
  // The first non-past period is "Current"; the one after it is "Next".
  readonly enrichedPeriods = computed((): PaymentPeriod[] => {
    const periods = this.schedule()?.allPeriods ?? [];
    const statusMap = this.paymentByDueDate();
    const PAID: PaymentStatus[] = ['Paid', 'PaidExternally', 'Refunded'];

    const withPast = periods.map(p => {
      const st = statusMap.get(p.dueDate);
      return { ...p, isPast: p.isPast || (!!st && PAID.includes(st)), isCurrent: false, isNext: false };
    });

    const ci = withPast.findIndex(p => !p.isPast);
    const ni = ci >= 0 ? ci + 1 : -1;
    return withPast.map((p, i) => ({ ...p, isCurrent: i === ci, isNext: i === ni && ni >= 0 }));
  });

  readonly enrichedCurrentPeriod = computed(() => this.enrichedPeriods().find(p => p.isCurrent) ?? null);
  readonly enrichedNextPeriod    = computed(() => this.enrichedPeriods().find(p => p.isNext)    ?? null);

  visiblePeriods = computed(() => {
    const all = this.enrichedPeriods();
    return this.showAll() ? all : all.slice(0, 6);
  });

  paidPayments   = computed(() => this.payments().filter(p => p.status === 'Paid'));
  pendingPayments = computed(() =>
    this.payments().filter(p => p.status === 'Pending' || p.status === 'Late' || p.status === 'Overdue')
  );

  ngOnInit() {
    this.loadSchedule();
    this.loadHistory();
  }

  loadSchedule() {
    this.scheduleLoading.set(true);
    this.scheduleError.set(null);
    this.paymentService.getTenantPaymentSchedule().subscribe({
      next: (s) => { this.schedule.set(s); this.scheduleLoading.set(false); },
      error: (err) => {
        const status = err?.status;
        if (status === 404)
          this.scheduleError.set('No active lease found. Payment schedule will appear once you have an active lease.');
        else if (status === 422)
          this.scheduleError.set('Your lease details are still being processed. Check back shortly.');
        else
          this.scheduleError.set('Payment schedule could not be loaded. Please try again.');
        this.scheduleLoading.set(false);
      },
    });
  }

  loadHistory() {
    this.historyLoading.set(true);
    this.historyError.set(null);
    this.paymentService.getTenantPayments().subscribe({
      next: (list) => { this.payments.set(list ?? []); this.historyLoading.set(false); },
      error: () => { this.historyError.set('Failed to load payment history.'); this.historyLoading.set(false); },
    });
  }

  /** Redirect the tenant to the Stripe Hosted Checkout page for this payment. */
  payNow(p: RentPayment): void {
    if (p.checkoutUrl) {
      window.location.href = p.checkoutUrl;
    }
  }

  /** True when a pending/overdue payment has a ready-to-use Stripe link. */
  hasCheckoutLink(p: RentPayment): boolean {
    return !!p.checkoutUrl && (p.status === 'Pending' || p.status === 'Late' || p.status === 'Overdue');
  }

  /** True when a payment is awaiting but the link hasn't been generated yet. */
  linkPending(p: RentPayment): boolean {
    return !p.checkoutUrl && (p.status === 'Pending' || p.status === 'Late' || p.status === 'Overdue');
  }

  totalDue(p: RentPayment): number {
    return (p.amount ?? 0) + (p.lateFee ?? 0);
  }

  /** 1% commission shown to tenant so they understand the total they'll pay. */
  commissionAmount(p: RentPayment): number {
    return Math.round(this.totalDue(p) * 0.01 * 100) / 100;
  }

  chargedAmount(p: RentPayment): number {
    return Math.round(this.totalDue(p) * 1.01 * 100) / 100;
  }

  periodStatusLabel(p: PaymentPeriod): string {
    if (p.isPast) return 'Past';
    if (p.isCurrent) return 'Current';
    if (p.isNext) return 'Next';
    return 'Upcoming';
  }

  periodColor(p: PaymentPeriod): string {
    if (p.isPast) return 'gray';
    if (p.isCurrent) return 'green';
    if (p.isNext) return 'amber';
    return 'blue';
  }

  statusColor(s: string): string {
    return ({ Pending: 'amber', Paid: 'green', Late: 'orange', Overdue: 'red', Refunded: 'blue' } as Record<string, string>)[s] ?? 'gray';
  }

  formatCurrency(n: number) {
    return `${(n ?? 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} AED`;
  }

  formatDate(iso: string) {
    if (!iso) return '—';
    return new Date(iso).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
  }

  progressPercent(s: PaymentSchedule): number {
    const total = s.summary.totalInstallments;
    const past  = s.summary.pastInstallments;
    return total > 0 ? Math.round((past / total) * 100) : 0;
  }
}
