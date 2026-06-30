import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PaymentService, PaymentSummaryDto } from '../../../../../core/services/payment.service';
import { RentPayment, PaymentStatus } from '../../../../../core/models/payment.model';

type Tab = 'all' | 'overdue';

@Component({
  selector: 'app-admin-payments',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './payments.component.html',
  styleUrls: ['./payments.component.scss'],
})
export class AdminPaymentsComponent implements OnInit {
  private paymentService = inject(PaymentService);

  activeTab = signal<Tab>('all');
  payments = signal<RentPayment[]>([]);
  overduePayments = signal<RentPayment[]>([]);
  loading = signal(false);
  loadingOverdue = signal(false);
  processing = signal<string | null>(null);
  toast = signal<{ type: 'success' | 'error'; text: string } | null>(null);
  searchQuery = signal('');
  statusFilter = signal<PaymentStatus | 'All'>('All');
  generating = signal(false);
  generateResult = signal<{ generatedCount: number; skippedCount: number } | null>(null);
  summary = signal<PaymentSummaryDto | null>(null);

  filteredPayments = computed(() => {
    let list = this.payments();
    const q = this.searchQuery().toLowerCase().trim();
    const sf = this.statusFilter();
    if (sf !== 'All') list = list.filter(p => p.status === sf);
    if (q) list = list.filter(p =>
      p.tenantName?.toLowerCase().includes(q) ||
      p.landlordName?.toLowerCase().includes(q) ||
      p.propertyTitle?.toLowerCase().includes(q) ||
      p.leaseId?.toLowerCase().includes(q),
    );
    return list;
  });

  stats = computed(() => {
    const all = this.payments();
    return {
      total: all.length,
      pending: all.filter(p => p.status === 'Pending').length,
      paid: all.filter(p => p.status === 'Paid').length,
      overdue: all.filter(p => p.status === 'Overdue').length,
      totalAmount: all.reduce((s, p) => s + p.amount, 0),
    };
  });

  ngOnInit() {
    this.loadPayments();
    this.loadOverdue();
    this.loadSummary();
  }

  loadSummary() {
    this.paymentService.getPaymentSummary().subscribe({
      next: (s) => this.summary.set(s),
    });
  }

  loadPayments() {
    this.loading.set(true);
    this.paymentService.getAllPayments().subscribe({
      next: (list) => { this.payments.set(list ?? []); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  loadOverdue() {
    this.loadingOverdue.set(true);
    this.paymentService.getOverduePayments().subscribe({
      next: (list) => { this.overduePayments.set(list ?? []); this.loadingOverdue.set(false); },
      error: () => this.loadingOverdue.set(false),
    });
  }

  generateMonthly() {
    this.generating.set(true);
    this.generateResult.set(null);
    this.paymentService.generateMonthly().subscribe({
      next: (res) => {
        this.generateResult.set(res);
        this.generating.set(false);
        this.showToast('success', `Generated ${res.generatedCount} payments, skipped ${res.skippedCount}`);
        this.loadPayments();
      },
      error: (err) => {
        this.generating.set(false);
        this.showToast('error', err?.error?.message ?? 'Failed to generate payments');
      },
    });
  }

  processPayment(p: RentPayment) {
    this.processing.set(p.id);
    this.paymentService.processPayment(p.id, { paymentMethod: 'Other' }).subscribe({
      next: (res) => {
        this.processing.set(null);
        if (res.isSuccess) {
          this.showToast('success', 'Payment processed successfully');
          this.loadPayments();
        } else {
          this.showToast('error', res.errorMessage ?? 'Processing failed');
        }
      },
      error: (err) => {
        this.processing.set(null);
        this.showToast('error', err?.error?.message ?? 'Failed to process payment');
      },
    });
  }

  applyLateFee(p: RentPayment) {
    this.processing.set(p.id);
    this.paymentService.applyLateFee(p.id).subscribe({
      next: () => {
        this.processing.set(null);
        this.showToast('success', 'Late fee applied');
        this.loadOverdue();
      },
      error: (err) => {
        this.processing.set(null);
        this.showToast('error', err?.error?.message ?? 'Failed to apply late fee');
      },
    });
  }

  markAsPaidExternally(p: RentPayment) {
    this.processing.set(p.id);
    this.paymentService.markAsPaidExternally(p.id).subscribe({
      next: () => {
        this.processing.set(null);
        this.showToast('success', 'Payment marked as paid externally (cash/real-world)');
        this.loadPayments();
        this.loadOverdue();
      },
      error: (err) => {
        this.processing.set(null);
        this.showToast('error', err?.error?.error ?? 'Failed to mark as externally paid');
      },
    });
  }

  generatingLink = signal<string | null>(null);

  generatePaymentLink(p: RentPayment) {
    this.generatingLink.set(p.id);
    this.paymentService.generateRentPaymentLink(p.id).subscribe({
      next: (res) => {
        this.generatingLink.set(null);
        this.showToast('success', 'Stripe link generated — refreshing list');
        // Update the payment in-place with the new checkout URL
        this.payments.update(list => list.map(x =>
          x.id === p.id ? { ...x, checkoutUrl: res.checkoutUrl } : x
        ));
      },
      error: (err) => {
        this.generatingLink.set(null);
        this.showToast('error', err?.error?.error ?? 'Failed to generate payment link');
      },
    });
  }

  statusColor(s: PaymentStatus): string {
    return ({
      Pending: 'amber',
      Paid: 'green',
      Late: 'orange',
      Overdue: 'red',
      Refunded: 'blue',
      PaidExternally: 'gray',
    } as Record<string, string>)[s] ?? 'gray';
  }

  formatCurrency(n: number) {
    return `${(n ?? 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} EGP`;
  }

  formatDate(iso: string) {
    if (!iso) return '—';
    return new Date(iso).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
  }

  daysSince(isoDate: string): number {
    if (!isoDate) return 0;
    const diff = Date.now() - new Date(isoDate).getTime();
    return Math.floor(diff / (1000 * 60 * 60 * 24));
  }

  showToast(type: 'success' | 'error', text: string) {
    this.toast.set({ type, text });
    setTimeout(() => this.toast.set(null), 3500);
  }
}
