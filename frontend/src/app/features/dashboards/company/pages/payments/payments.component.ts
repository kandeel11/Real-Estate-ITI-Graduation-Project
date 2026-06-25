import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PaymentService } from '../../../../../core/services/payment.service';
import { RentPayment, BrokerLeaseSchedule, PaymentPeriod } from '../../../../../core/models/payment.model';

type ActiveTab = 'payments' | 'schedule';

@Component({
  selector: 'app-company-payments',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './payments.component.html',
  styleUrls: ['./payments.component.scss'],
})
export class CompanyPaymentsComponent implements OnInit {
  private paymentService = inject(PaymentService);

  activeTab = signal<ActiveTab>('payments');

  // Payments tab
  paymentsLoading = signal(false);
  paymentsError = signal<string | null>(null);
  payments = signal<RentPayment[]>([]);

  // Schedule tab
  scheduleLoading = signal(false);
  scheduleError = signal<string | null>(null);
  leaseSchedules = signal<BrokerLeaseSchedule[]>([]);
  selectedLeaseId = signal<string | null>(null);
  showAll = signal(false);

  selectedLeaseSchedule = computed(() =>
    this.leaseSchedules().find(ls => ls.leaseId === this.selectedLeaseId()) ?? null
  );

  visiblePeriods = computed(() => {
    const all = this.selectedLeaseSchedule()?.schedule.allPeriods ?? [];
    return this.showAll() ? all : all.slice(0, 6);
  });

  paymentStats = computed(() => {
    const all = this.payments();
    const byProperty = all.reduce((acc, p) => {
      const key = p.propertyTitle ?? p.propertyId ?? 'Unknown';
      if (!acc[key]) acc[key] = { paid: 0, pending: 0, total: 0, amount: 0 };
      acc[key].total++;
      acc[key].amount += p.amount;
      if (p.status === 'Paid') acc[key].paid++;
      else if (p.status === 'Pending' || p.status === 'Late' || p.status === 'Overdue') acc[key].pending++;
      return acc;
    }, {} as Record<string, { paid: number; pending: number; total: number; amount: number }>);

    return {
      total: all.length,
      paid: all.filter(p => p.status === 'Paid').length,
      pending: all.filter(p => p.status === 'Pending').length,
      overdue: all.filter(p => p.status === 'Overdue' || p.status === 'Late').length,
      totalAmount: all.reduce((s, p) => s + p.amount, 0),
      byProperty,
    };
  });

  ngOnInit() {
    this.loadPayments();
    this.loadSchedules();
  }

  loadPayments() {
    this.paymentsLoading.set(true);
    this.paymentsError.set(null);
    this.paymentService.getCompanyPayments().subscribe({
      next: (list) => { this.payments.set(list ?? []); this.paymentsLoading.set(false); },
      error: () => { this.paymentsError.set('Could not load payments.'); this.paymentsLoading.set(false); },
    });
  }

  loadSchedules() {
    this.scheduleLoading.set(true);
    this.scheduleError.set(null);
    this.paymentService.getCompanyPaymentSchedules().subscribe({
      next: (list) => {
        this.leaseSchedules.set(list ?? []);
        if (list?.length) this.selectedLeaseId.set(list[0].leaseId);
        this.scheduleLoading.set(false);
      },
      error: () => { this.scheduleError.set('Could not load schedules.'); this.scheduleLoading.set(false); },
    });
  }

  selectLease(leaseId: string) {
    if (this.selectedLeaseId() === leaseId) return;
    this.selectedLeaseId.set(leaseId);
    this.showAll.set(false);
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

  statusColor(s: string): string {
    return ({ Pending: 'amber', Paid: 'green', Late: 'orange', Overdue: 'red', Refunded: 'blue' } as Record<string, string>)[s] ?? 'gray';
  }

  progressPercent(ls: BrokerLeaseSchedule): number {
    const total = ls.schedule.summary.totalInstallments;
    const past = ls.schedule.summary.pastInstallments;
    return total > 0 ? Math.round((past / total) * 100) : 0;
  }

  propertyEntries() {
    return Object.entries(this.paymentStats().byProperty);
  }

  formatCurrency(n: number) {
    return `${(n ?? 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} EGP`;
  }

  formatDate(iso: string) {
    if (!iso) return '—';
    return new Date(iso).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
  }
}
