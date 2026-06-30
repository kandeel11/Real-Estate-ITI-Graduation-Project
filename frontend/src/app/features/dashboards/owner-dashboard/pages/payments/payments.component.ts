import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PaymentService } from '../../../../../core/services/payment.service';
import { RentPayment } from '../../../../../core/models/payment.model';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-owner-payments',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './payments.component.html',
  styleUrls: ['./payments.component.scss'],
})
export class OwnerPaymentsComponent implements OnInit {
  private paymentService = inject(PaymentService);

  loading  = signal(false);
  error    = signal<string | null>(null);
  payments = signal<RentPayment[]>([]);

  waivedLoading = signal<string | null>(null);

  stats = computed(() => {
    const all = this.payments();
    return {
      total:   all.length,
      paid:    all.filter(p => p.status === 'Paid').length,
      pending: all.filter(p => p.status === 'Pending').length,
      late:    all.filter(p => p.status === 'Late' || p.status === 'Overdue').length,
      totalAmount:    all.reduce((s, p) => s + p.amount, 0),
      totalLateFees:  all.reduce((s, p) => s + (p.lateFee ?? 0), 0),
      totalCollected: all.filter(p => p.status === 'Paid').reduce((s, p) => s + p.amount, 0),
    };
  });

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.error.set(null);
    this.paymentService.getMyPayments().pipe(
      finalize(() => this.loading.set(false))
    ).subscribe({
      next: list => this.payments.set(list ?? []),
      error: () => this.error.set('Could not load rent payments. Please try again.'),
    });
  }

  canWaive(p: RentPayment): boolean {
    return (p.status === 'Late' || p.status === 'Overdue') && (p.lateFee ?? 0) > 0;
  }

  waiveLateFee(p: RentPayment) {
    if (!confirm(`Waive the late fee of ${this.fmt(p.lateFee ?? 0)} for this payment? The tenant's checkout link will be regenerated at the base amount.`)) return;
    this.waivedLoading.set(p.id);
    this.paymentService.waiveLateFee(p.id).pipe(
      finalize(() => this.waivedLoading.set(null))
    ).subscribe({
      next: () => this.load(),
      error: () => alert('Could not waive late fee. Make sure you are the landlord for this property.'),
    });
  }

  statusColor(s: string): string {
    return ({ Pending: 'amber', Paid: 'green', Late: 'orange', Overdue: 'red', Refunded: 'blue' } as Record<string, string>)[s] ?? 'gray';
  }

  fmt(n: number) {
    return `${(n ?? 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} EGP`;
  }

  fmtDate(iso: string | undefined | null) {
    if (!iso) return '—';
    return new Date(iso).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
  }
}
