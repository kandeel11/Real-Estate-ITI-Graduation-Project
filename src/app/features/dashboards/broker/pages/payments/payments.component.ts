import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PaymentService } from '../../../../../core/services/payment.service';
import { BrokerDataService, BrokerLease } from '../../services/broker-data.service';
import { PaymentSchedule, PaymentPeriod } from '../../../../../core/models/payment.model';

@Component({
  selector: 'app-broker-payments',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './payments.component.html',
  styleUrls: ['./payments.component.scss'],
})
export class BrokerPaymentsComponent implements OnInit {
  private paymentService = inject(PaymentService);
  private brokerService = inject(BrokerDataService);

  leasesLoading = signal(true);
  scheduleLoading = signal(false);
  leases = signal<BrokerLease[]>([]);
  selectedLeaseId = signal<string | null>(null);
  schedule = signal<PaymentSchedule | null>(null);
  scheduleError = signal<string | null>(null);
  showAll = signal(false);

  selectedLease = computed(() =>
    this.leases().find(l => l.id === this.selectedLeaseId()) ?? null
  );

  visiblePeriods = computed(() => {
    const all = this.schedule()?.allPeriods ?? [];
    return this.showAll() ? all : all.slice(0, 6);
  });

  activeLeases = computed(() =>
    this.leases().filter(l => l.status === 'active' || l.status === 'expiring')
  );

  ngOnInit() {
    this.brokerService.getLeases().subscribe({
      next: (ls) => {
        this.leases.set(ls);
        this.leasesLoading.set(false);
        const first = ls.find(l => l.status === 'active' || l.status === 'expiring');
        if (first) this.selectLease(first.id);
      },
      error: () => this.leasesLoading.set(false),
    });
  }

  selectLease(id: string) {
    if (this.selectedLeaseId() === id) return;
    this.selectedLeaseId.set(id);
    this.schedule.set(null);
    this.scheduleError.set(null);
    this.showAll.set(false);
    this.scheduleLoading.set(true);
    this.paymentService.getPaymentSchedule(id).subscribe({
      next: (s) => { this.schedule.set(s); this.scheduleLoading.set(false); },
      error: () => {
        this.scheduleError.set('Payment schedule is not available for this lease yet.');
        this.scheduleLoading.set(false);
      },
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

  progressPercent(s: PaymentSchedule): number {
    const total = s.summary.totalInstallments;
    const past = s.summary.pastInstallments;
    return total > 0 ? Math.round((past / total) * 100) : 0;
  }

  formatCurrency(n: number) {
    return `${(n ?? 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} AED`;
  }

  formatDate(iso: string) {
    if (!iso) return '—';
    return new Date(iso).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
  }

  leaseStatusColor(status: string): string {
    if (status === 'active') return 'green';
    if (status === 'expiring') return 'amber';
    return 'gray';
  }
}
