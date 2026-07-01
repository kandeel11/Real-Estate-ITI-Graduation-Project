import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PaymentService } from '../../../../../core/services/payment.service';
import {
  RentPayment,
  BrokerLeaseSchedule,
  PaymentPeriod,
  OwnerFinancialSummary,
} from '../../../../../core/models/payment.model';
import { finalize } from 'rxjs';

type ActiveTab = 'payments' | 'schedule';

@Component({
  selector: 'app-owner-payments',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './payments.component.html',
  styleUrls: ['./payments.component.scss'],
})
export class OwnerPaymentsComponent implements OnInit {
  private paymentService = inject(PaymentService);

  activeTab = signal<ActiveTab>('payments');

  // Payments tab
  paymentsLoading = signal(false);
  paymentsError = signal<string | null>(null);
  payments = signal<RentPayment[]>([]);

  // Financial summary (real revenue data)
  summaryLoading = signal(false);
  summary = signal<OwnerFinancialSummary | null>(null);

  // Schedule tab
  scheduleLoading = signal(false);
  scheduleError = signal<string | null>(null);
  leaseSchedules = signal<BrokerLeaseSchedule[]>([]);
  selectedLeaseId = signal<string | null>(null);
  showAll = signal(false);

  // Waive tracking
  waivedIds = signal<Set<string>>(new Set());
  waivedLoading = signal<string | null>(null);

  selectedLeaseSchedule = computed(() =>
    this.leaseSchedules().find(ls => ls.leaseId === this.selectedLeaseId()) ?? null
  );

  visiblePeriods = computed(() => {
    const all = this.selectedLeaseSchedule()?.schedule.allPeriods ?? [];
    return this.showAll() ? all : all.slice(0, 6);
  });

  stats = computed(() => {
    const all = this.payments();
    const s = this.summary();
    const isPaid = (p: RentPayment) => p.status === 'Paid' || p.status === 'PaidExternally';
    const byProperty = all.reduce((acc, p) => {
      const key = p.propertyTitle ?? p.propertyId ?? 'Unknown';
      if (!acc[key]) acc[key] = { paid: 0, pending: 0, total: 0, amount: 0 };
      acc[key].total++;
      acc[key].amount += p.amount;
      if (isPaid(p)) acc[key].paid++;
      else acc[key].pending++;
      return acc;
    }, {} as Record<string, { paid: number; pending: number; total: number; amount: number }>);

    return {
      totalCollected: s?.totalRevenue ?? all.filter(isPaid).reduce((sum, p) => sum + p.amount, 0),
      paidCount: all.filter(isPaid).length,
      pending: all.filter(p => p.status === 'Pending').length,
      overdue: all.filter(p => p.status === 'Late' || p.status === 'Overdue').length,
      totalLateFees: s?.totalLateFees ?? all.reduce((sum, p) => sum + (p.lateFee ?? 0), 0),
      totalRecords: all.length,
      byProperty,
    };
  });

  ngOnInit() {
    this.loadPayments();
    this.loadSummary();
    this.loadSchedules();
  }

  loadPayments() {
    this.paymentsLoading.set(true);
    this.paymentsError.set(null);
    this.paymentService.getMyPayments().pipe(
      finalize(() => this.paymentsLoading.set(false))
    ).subscribe({
      next: list => this.payments.set(list ?? []),
      error: () => this.paymentsError.set('Could not load rent payments. Please try again.'),
    });
  }

  loadSummary() {
    this.summaryLoading.set(true);
    this.paymentService.getOwnerFinancialSummary().pipe(
      finalize(() => this.summaryLoading.set(false))
    ).subscribe({
      next: s => this.summary.set(s),
      error: () => { /* non-critical — stats fall back to computed values */ },
    });
  }

  loadSchedules() {
    this.scheduleLoading.set(true);
    this.scheduleError.set(null);
    this.paymentService.getOwnerPaymentSchedules().pipe(
      finalize(() => this.scheduleLoading.set(false))
    ).subscribe({
      next: list => {
        this.leaseSchedules.set(list ?? []);
        if (list?.length) this.selectedLeaseId.set(list[0].leaseId);
      },
      error: () => this.scheduleError.set('Could not load payment schedules.'),
    });
  }

  reload() {
    this.loadPayments();
    this.loadSummary();
    this.loadSchedules();
  }

  selectLease(leaseId: string) {
    if (this.selectedLeaseId() === leaseId) return;
    this.selectedLeaseId.set(leaseId);
    this.showAll.set(false);
  }

  canWaive(p: RentPayment): boolean {
    return (p.status === 'Late' || p.status === 'Overdue') &&
      (p.lateFee ?? 0) > 0 &&
      !this.waivedIds().has(p.id);
  }

  waiveLateFee(p: RentPayment) {
    if (!confirm(`Waive the late fee of ${this.fmt(p.lateFee ?? 0)} for this payment? The tenant's checkout link will be regenerated at the base amount.`)) return;
    this.waivedLoading.set(p.id);
    this.paymentService.waiveLateFee(p.id).pipe(
      finalize(() => this.waivedLoading.set(null))
    ).subscribe({
      next: () => {
        this.waivedIds.update(s => { const n = new Set(s); n.add(p.id); return n; });
        this.loadPayments();
      },
      error: () => alert('Could not waive late fee. Make sure you are the landlord for this property.'),
    });
  }

  periodColor(p: PaymentPeriod): string {
    if (p.isPast) return 'gray';
    if (p.isCurrent) return 'green';
    if (p.isNext) return 'amber';
    return 'blue';
  }

  periodStatusLabel(p: PaymentPeriod): string {
    if (p.isPast) return 'Past';
    if (p.isCurrent) return 'Current';
    if (p.isNext) return 'Next';
    return 'Upcoming';
  }

  progressPercent(ls: BrokerLeaseSchedule): number {
    const total = ls.schedule.summary.totalInstallments;
    const past = ls.schedule.summary.pastInstallments;
    return total > 0 ? Math.round((past / total) * 100) : 0;
  }

  statusColor(s: string): string {
    return ({
      Pending: 'amber', Paid: 'green', PaidExternally: 'teal',
      Late: 'orange', Overdue: 'red', Refunded: 'blue',
    } as Record<string, string>)[s] ?? 'gray';
  }

  propertyEntries() {
    return Object.entries(this.stats().byProperty);
  }

  fmt(n: number) {
    return `${(n ?? 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} EGP`;
  }

  fmtDate(iso: string | undefined | null) {
    if (!iso) return '—';
    return new Date(iso).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
  }
}
