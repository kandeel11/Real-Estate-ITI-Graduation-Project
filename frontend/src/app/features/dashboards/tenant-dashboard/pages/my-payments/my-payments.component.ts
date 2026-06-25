import { Component, inject, signal, computed, OnInit, ElementRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PaymentService } from '../../../../../core/services/payment.service';
import { RentPayment, PaymentSchedule, PaymentPeriod } from '../../../../../core/models/payment.model';

/* eslint-disable @typescript-eslint/no-explicit-any */
declare const Stripe: any;

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

  @ViewChild('stripeCard') stripeCardRef?: ElementRef<HTMLDivElement>;

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

  visiblePeriods = computed(() => {
    const all = this.schedule()?.allPeriods ?? [];
    return this.showAll() ? all : all.slice(0, 6);
  });

  paidPayments = computed(() => this.payments().filter(p => p.status === 'Paid'));
  pendingPayments = computed(() => this.payments().filter(p => p.status === 'Pending' || p.status === 'Late' || p.status === 'Overdue'));

  // Stripe Elements state
  stripePaymentId = signal<string | null>(null);
  stripeLoading = signal(false);
  stripeError = signal<string | null>(null);
  stripeConfirming = signal(false);
  stripeSuccess = signal(false);
  private stripeInstance: any = null;
  private stripeElements: any = null;
  private stripePaymentEl: any = null;

  ngOnInit() {
    this.loadSchedule();
    this.loadHistory();
  }

  loadSchedule() {
    this.scheduleLoading.set(true);
    this.scheduleError.set(null);
    // Uses GET /api/tenant/payments/schedule — no lease ID needed
    this.paymentService.getTenantPaymentSchedule().subscribe({
      next: (s) => { this.schedule.set(s); this.scheduleLoading.set(false); },
      error: (err) => {
        const status = err?.status;
        if (status === 404) {
          this.scheduleError.set('No active lease found. Payment schedule will appear once you have an active lease.');
        } else if (status === 422) {
          this.scheduleError.set('Your lease details are still being processed by our AI. Check back shortly.');
        } else {
          this.scheduleError.set('Payment schedule could not be loaded. Please try again.');
        }
        this.scheduleLoading.set(false);
      },
    });
  }

  loadHistory() {
    this.historyLoading.set(true);
    this.historyError.set(null);
    // Uses GET /api/tenant/payments
    this.paymentService.getTenantPayments().subscribe({
      next: (list) => { this.payments.set(list ?? []); this.historyLoading.set(false); },
      error: () => { this.historyError.set('Failed to load payment history.'); this.historyLoading.set(false); },
    });
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
    return `${(n ?? 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} EGP`;
  }

  formatDate(iso: string) {
    if (!iso) return '—';
    return new Date(iso).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
  }

  progressPercent(s: PaymentSchedule): number {
    const total = s.summary.totalInstallments;
    const past = s.summary.pastInstallments;
    return total > 0 ? Math.round((past / total) * 100) : 0;
  }

  // ── Stripe Elements rent payment ─────────────────────────────────────────

  openStripePayment(paymentId: string): void {
    this.stripePaymentId.set(paymentId);
    this.stripeError.set(null);
    this.stripeSuccess.set(false);
    this.stripeLoading.set(true);
    this.paymentService.initiatePayment(paymentId).subscribe({
      next: ({ clientSecret, publishableKey }) => {
        this.loadStripeJs(publishableKey, clientSecret);
      },
      error: () => {
        this.stripeError.set('Could not start payment. Please try again.');
        this.stripeLoading.set(false);
      },
    });
  }

  private loadStripeJs(publishableKey: string, clientSecret: string): void {
    if (typeof Stripe !== 'undefined') {
      this.mountStripe(publishableKey, clientSecret);
      return;
    }
    const script = document.createElement('script');
    script.src = 'https://js.stripe.com/v3/';
    script.onload = () => this.mountStripe(publishableKey, clientSecret);
    script.onerror = () => {
      this.stripeError.set('Failed to load Stripe. Check your connection and try again.');
      this.stripeLoading.set(false);
    };
    document.head.appendChild(script);
  }

  private mountStripe(publishableKey: string, clientSecret: string): void {
    this.stripeLoading.set(false);
    // Give the DOM time to render the mount point
    setTimeout(() => {
      const mountEl = this.stripeCardRef?.nativeElement;
      if (!mountEl) return;
      this.stripeInstance = Stripe(publishableKey);
      this.stripeElements = this.stripeInstance.elements({ clientSecret });
      this.stripePaymentEl = this.stripeElements.create('payment');
      this.stripePaymentEl.mount(mountEl);
    }, 50);
  }

  async confirmStripePayment(): Promise<void> {
    if (!this.stripeInstance || !this.stripeElements || this.stripeConfirming()) return;
    this.stripeConfirming.set(true);
    this.stripeError.set(null);
    const { error } = await this.stripeInstance.confirmPayment({
      elements: this.stripeElements,
      confirmParams: { return_url: window.location.href },
      redirect: 'if_required',
    });
    this.stripeConfirming.set(false);
    if (error) {
      this.stripeError.set(error.message ?? 'Payment failed. Please try again.');
    } else {
      this.stripeSuccess.set(true);
      this.closeStripePayment();
      this.loadHistory();
    }
  }

  closeStripePayment(): void {
    this.stripePaymentEl?.unmount();
    this.stripePaymentEl = null;
    this.stripeElements = null;
    this.stripeInstance = null;
    this.stripePaymentId.set(null);
    this.stripeError.set(null);
    this.stripeLoading.set(false);
    this.stripeConfirming.set(false);
  }
}
