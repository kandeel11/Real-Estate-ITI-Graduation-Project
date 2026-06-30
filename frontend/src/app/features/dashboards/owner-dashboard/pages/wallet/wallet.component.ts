import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { WalletService } from '../../../../../core/services/wallet.service';
import {
  WalletBalance,
  LedgerEntry,
  WithdrawalRequest,
} from '../../../../../core/models/wallet.model';

type View = 'overview' | 'ledger' | 'withdraw' | 'history';

@Component({
  selector: 'app-owner-wallet',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './wallet.component.html',
  styleUrls: ['./wallet.component.scss'],
})
export class OwnerWalletComponent implements OnInit {
  private walletService = inject(WalletService);

  activeView = signal<View>('overview');

  // Balance
  balance = signal<WalletBalance | null>(null);
  loadingBalance = signal(false);

  // Ledger
  ledgerEntries = signal<LedgerEntry[]>([]);
  ledgerTotal = signal(0);
  loadingLedger = signal(false);
  ledgerPage = signal(1);
  ledgerPageSize = 15;

  // Withdrawals
  withdrawals = signal<WithdrawalRequest[]>([]);
  loadingWithdrawals = signal(false);

  // Withdrawal form
  withdrawAmount = signal('');
  withdrawBank = signal('');
  submitting = signal(false);
  toast = signal<{ type: 'success' | 'error'; text: string } | null>(null);

  readonly parseFloat = parseFloat;

  commissionPreview = computed(() => {
    const amt = parseFloat(this.withdrawAmount()) || 0;
    return { commission: amt * 0.01, net: amt * 0.99 };
  });

  canWithdraw = computed(() => {
    const amt = parseFloat(this.withdrawAmount()) || 0;
    const bal = this.balance()?.balance ?? 0;
    return amt > 0 && amt <= bal && this.withdrawBank().trim().length >= 5;
  });

  totalCredits = computed(() =>
    this.ledgerEntries().filter(e => e.type === 'Credit').reduce((s, e) => s + e.amount, 0),
  );

  totalDebits = computed(() =>
    this.ledgerEntries().filter(e => e.type === 'Debit').reduce((s, e) => s + e.amount, 0),
  );

  pendingWithdrawalsCount = computed(() =>
    this.withdrawals().filter(w => w.status === 'Pending').length,
  );

  completedWithdrawalsCount = computed(() =>
    this.withdrawals().filter(w => w.status === 'Completed').length,
  );

  ngOnInit() {
    this.loadBalance();
    this.loadLedger();
    this.loadWithdrawals();
  }

  setView(v: View) {
    this.activeView.set(v);
  }

  loadBalance() {
    this.loadingBalance.set(true);
    this.walletService.getBalance().subscribe({
      next: (b) => { this.balance.set(b); this.loadingBalance.set(false); },
      error: () => this.loadingBalance.set(false),
    });
  }

  loadLedger() {
    this.loadingLedger.set(true);
    this.walletService.getLedger(this.ledgerPage(), this.ledgerPageSize).subscribe({
      next: (r) => {
        this.ledgerEntries.set(r.items ?? []);
        this.ledgerTotal.set(r.total ?? 0);
        this.loadingLedger.set(false);
      },
      error: () => this.loadingLedger.set(false),
    });
  }

  loadWithdrawals() {
    this.loadingWithdrawals.set(true);
    this.walletService.getMyWithdrawals().subscribe({
      next: (w) => { this.withdrawals.set(w ?? []); this.loadingWithdrawals.set(false); },
      error: () => this.loadingWithdrawals.set(false),
    });
  }

  submitWithdrawal() {
    const amt = parseFloat(this.withdrawAmount());
    const bank = this.withdrawBank().trim();
    if (!this.canWithdraw()) return;
    this.submitting.set(true);
    this.walletService.requestWithdrawal({ amount: amt, bankAccountDetails: bank }).subscribe({
      next: () => {
        this.showToast('success', 'Withdrawal request submitted successfully');
        this.withdrawAmount.set('');
        this.withdrawBank.set('');
        this.submitting.set(false);
        this.loadBalance();
        this.loadWithdrawals();
        this.activeView.set('history');
      },
      error: (err) => {
        this.showToast('error', err?.error?.message ?? 'Failed to submit withdrawal');
        this.submitting.set(false);
      },
    });
  }

  changeLedgerPage(p: number) {
    this.ledgerPage.set(p);
    this.loadLedger();
  }

  ledgerTotalPages = computed(() =>
    Math.max(1, Math.ceil(this.ledgerTotal() / this.ledgerPageSize)),
  );

  formatCurrency(n: number, _currency = 'EGP') {
    // Platform currency is always EGP regardless of any legacy stored currency.
    return `${n.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} EGP`;
  }

  formatDate(iso: string) {
    if (!iso) return '—';
    return new Date(iso).toLocaleDateString('en-US', {
      year: 'numeric', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit',
    });
  }

  statusColor(s: string) {
    return { Pending: 'amber', Completed: 'green', Rejected: 'red' }[s] ?? 'gray';
  }

  showToast(type: 'success' | 'error', text: string) {
    this.toast.set({ type, text });
    setTimeout(() => this.toast.set(null), 3500);
  }
}
