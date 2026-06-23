import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { WalletService } from '../../../../../core/services/wallet.service';
import { WithdrawalRequest, WithdrawalStatus } from '../../../../../core/models/wallet.model';

@Component({
  selector: 'app-admin-withdrawals',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './withdrawals.component.html',
  styleUrls: ['./withdrawals.component.scss'],
})
export class AdminWithdrawalsComponent implements OnInit {
  private walletService = inject(WalletService);

  withdrawals = signal<WithdrawalRequest[]>([]);
  loading = signal(false);
  processing = signal<string | null>(null);
  toast = signal<{ type: 'success' | 'error'; text: string } | null>(null);
  statusFilter = signal<WithdrawalStatus | 'All'>('All');
  searchQuery = signal('');

  filtered = computed(() => {
    let list = this.withdrawals();
    const sf = this.statusFilter();
    const q = this.searchQuery().toLowerCase().trim();
    if (sf !== 'All') list = list.filter(w => w.status === sf);
    if (q) list = list.filter(w =>
      w.ownerName?.toLowerCase().includes(q) ||
      w.bankAccountDetails?.toLowerCase().includes(q) ||
      w.ownerId?.toLowerCase().includes(q),
    );
    return list;
  });

  stats = computed(() => {
    const all = this.withdrawals();
    return {
      total: all.length,
      pending: all.filter(w => w.status === 'Pending').length,
      completed: all.filter(w => w.status === 'Completed').length,
      rejected: all.filter(w => w.status === 'Rejected').length,
      totalVolume: all.filter(w => w.status === 'Completed').reduce((s, w) => s + w.amount, 0),
    };
  });

  ngOnInit() {
    this.load();
  }

  load() {
    this.loading.set(true);
    this.walletService.getAllWithdrawals().subscribe({
      next: (list) => { this.withdrawals.set(list ?? []); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  approve(w: WithdrawalRequest) {
    this.processing.set(w.id);
    this.walletService.approveWithdrawal(w.id).subscribe({
      next: () => {
        this.processing.set(null);
        this.showToast('success', `Withdrawal approved — ${this.formatCurrency(w.netAmount)} will be sent to owner`);
        this.load();
      },
      error: (err) => {
        this.processing.set(null);
        this.showToast('error', err?.error?.message ?? 'Failed to approve withdrawal');
      },
    });
  }

  reject(w: WithdrawalRequest) {
    this.processing.set(w.id);
    this.walletService.rejectWithdrawal(w.id).subscribe({
      next: () => {
        this.processing.set(null);
        this.showToast('success', 'Withdrawal rejected');
        this.load();
      },
      error: (err) => {
        this.processing.set(null);
        this.showToast('error', err?.error?.message ?? 'Failed to reject withdrawal');
      },
    });
  }

  statusColor(s: WithdrawalStatus): string {
    return { Pending: 'amber', Completed: 'green', Rejected: 'red' }[s] ?? 'gray';
  }

  formatCurrency(n: number) {
    return `${(n ?? 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} EGP`;
  }

  formatDate(iso: string) {
    if (!iso) return '—';
    return new Date(iso).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
  }

  showToast(type: 'success' | 'error', text: string) {
    this.toast.set({ type, text });
    setTimeout(() => this.toast.set(null), 3500);
  }
}
