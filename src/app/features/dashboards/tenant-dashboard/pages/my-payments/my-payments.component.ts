import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PaymentService } from '../../../../../core/services/payment.service';
import { LeaseService } from '../../services/lease.service';
import { PaymentSchedule, PaymentPeriod } from '../../../../../core/models/payment.model';

@Component({
  selector: 'app-my-payments',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './my-payments.component.html',
  styleUrls: ['./my-payments.component.scss'],
})
export class MyPaymentsComponent implements OnInit {
  private paymentService = inject(PaymentService);
  private leaseService = inject(LeaseService);

  loading = signal(false);
  error = signal<string | null>(null);
  schedule = signal<PaymentSchedule | null>(null);
  activeLeaseId = signal<string | null>(null);
  showAll = signal(false);

  visiblePeriods = computed(() => {
    const all = this.schedule()?.allPeriods ?? [];
    return this.showAll() ? all : all.slice(0, 6);
  });

  ngOnInit() {
    this.loading.set(true);
    this.leaseService.getLeases().subscribe({
      next: (leases) => {
        const active = leases.find(l => l.isActive);
        if (!active) {
          this.error.set('No active lease found. Payment schedule is only available for active leases.');
          this.loading.set(false);
          return;
        }
        this.activeLeaseId.set(active.id);
        this.loadSchedule(active.id);
      },
      error: () => {
        this.error.set('Failed to load lease information.');
        this.loading.set(false);
      },
    });
  }

  loadSchedule(leaseId: string) {
    this.paymentService.getPaymentSchedule(leaseId).subscribe({
      next: (s) => { this.schedule.set(s); this.loading.set(false); },
      error: () => {
        this.error.set('Payment schedule is not available for this lease yet.');
        this.loading.set(false);
      },
    });
  }

  periodStatus(p: PaymentPeriod): string {
    if (p.isPast) return 'past';
    if (p.isCurrent) return 'current';
    if (p.isNext) return 'next';
    return 'future';
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

  formatCurrency(n: number) {
    return `${(n ?? 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} AED`;
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
}
