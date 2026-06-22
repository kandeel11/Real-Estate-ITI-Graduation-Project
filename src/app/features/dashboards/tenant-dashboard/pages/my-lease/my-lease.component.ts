import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe, CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { forkJoin } from 'rxjs';
import { finalize } from 'rxjs';
import { LeaseService, ApiLease, LeaseOffer, LeaseLifecycleStatus } from '../../services/lease.service';
import { AiService } from '../../services/ai.service';

/** Show the renewal CTA once the lease is within this many days of its end. */
const RENEWAL_WINDOW_DAYS = 30;

interface LeaseView {
  id: string;
  propertyId: string;
  propertyAddress: string;
  installmentAmount: number;
  totalRentAmount?: number | null;
  paymentFrequency?: string | null;
  securityDeposit?: number | null;
  startDate: string;
  endDate: string;
  paymentDayOfMonth: number;
  isActive: boolean;
  /** Lifecycle status as derived by the backend. */
  status: LeaseLifecycleStatus;
  contractFileUrl: string;
  daysUntilExpiry: number;
  /** True when the pending termination request was raised by the tenant (this side). */
  terminationRequestedByTenant: boolean;
}

interface ChatEntry {
  role: 'user' | 'assistant';
  content: string;
}

@Component({
  selector: 'app-my-lease',
  standalone: true,
  imports: [DatePipe, CurrencyPipe, FormsModule, TranslatePipe],
  templateUrl: './my-lease.component.html',
  styleUrl: './my-lease.component.css',
})
export class MyLeaseComponent implements OnInit {
  private readonly leaseService = inject(LeaseService);
  private readonly aiService = inject(AiService);

  protected activeTab = signal<'active' | 'history'>('active');
  protected loading = signal(true);
  protected error = signal<string | null>(null);
  protected activeLease = signal<LeaseView | null>(null);
  protected leaseHistory = signal<LeaseView[]>([]);
  protected expandedLeaseId = signal<string | null>(null);

  protected pendingOffer = signal<LeaseOffer | null>(null);
  protected offerActionInProgress = signal(false);

  protected chatMessages = signal<ChatEntry[]>([]);
  protected chatInput = '';
  protected chatLoading = signal(false);

  protected contractLoading = signal<boolean>(false);

  // ── Renewal / termination actions ──────────────────────────────────────────
  protected actionInProgress = signal(false);
  protected actionError = signal<string | null>(null);
  protected showTerminationForm = signal(false);
  protected terminationReason = '';
  protected terminationEffectiveDate = '';

  ngOnInit(): void {
    this.loadData();
    this.leaseService.getPendingOffer().subscribe(offer => this.pendingOffer.set(offer));
  }

  loadData(): void {
    this.loading.set(true);
    this.error.set(null);

    forkJoin({
      leases: this.leaseService.getLeases(),
      properties: this.leaseService.getRentedProperties(),
    })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: ({ leases, properties }) => {
          // Build leaseId → address map from rented-properties
          const addressMap = new Map<string, string>(
            properties.map(p => {
              const parts = [p.property.address, p.property.district, p.property.city].filter(Boolean);
              return [p.leaseId, parts.join(', ')];
            })
          );

          const views = leases.map(l => this.toView(l, addressMap.get(l.id)));
          this.leaseHistory.set(views);
          this.activeLease.set(views.find(l => l.isActive) ?? null);
        },
        error: () => this.error.set('Failed to load lease data. Please try again.'),
      });
  }

  private toView(lease: ApiLease, address?: string): LeaseView {
    const end = new Date(lease.endDate).getTime();
    const daysUntilExpiry = Math.max(0, Math.ceil((end - Date.now()) / 86_400_000));

    return {
      id: lease.id,
      propertyId: lease.propertyId,
      propertyAddress: address ?? `Property ${lease.propertyId.slice(-6)}`,
      installmentAmount: lease.installmentAmount,
      totalRentAmount: lease.totalRentAmount,
      paymentFrequency: lease.paymentFrequency,
      securityDeposit: lease.securityDeposit,
      startDate: lease.startDate,
      endDate: lease.endDate,
      paymentDayOfMonth: lease.paymentDayOfMonth,
      isActive: lease.isActive,
      // Trust the backend's derived status; fall back to isActive for older payloads.
      status: lease.status ?? (lease.isActive ? 'active' : 'ended'),
      contractFileUrl: lease.contractFileUrl,
      daysUntilExpiry,
      terminationRequestedByTenant: lease.terminationRequestedById === lease.tenantId,
    };
  }

  /** The renewal CTA shows for an active lease within the renewal window with no request pending. */
  canRequestRenewal(lease: LeaseView): boolean {
    return lease.status === 'active' && lease.daysUntilExpiry <= RENEWAL_WINDOW_DAYS;
  }

  statusLabelKey(status: LeaseLifecycleStatus): string {
    switch (status) {
      case 'active': return 'MY_LEASE.ACTIVE';
      case 'pending': return 'MY_LEASE.PENDING';
      case 'renewal_requested': return 'MY_LEASE.STATUS_RENEWAL_REQUESTED';
      case 'termination_requested': return 'MY_LEASE.STATUS_TERMINATION_REQUESTED';
      case 'terminated': return 'MY_LEASE.STATUS_TERMINATED';
      default: return 'MY_LEASE.ENDED';
    }
  }

  /** 'active' | 'pending' | 'ended' — drives the existing badge colour classes. */
  badgeTone(status: LeaseLifecycleStatus): 'active' | 'pending' | 'ended' {
    if (status === 'pending') return 'pending';
    if (status === 'terminated' || status === 'ended') return 'ended';
    return 'active';
  }

  leaseElapsedPercent(lease: LeaseView): number {
    const start = new Date(lease.startDate).getTime();
    const end = new Date(lease.endDate).getTime();
    return Math.min(100, Math.max(0, Math.round(((Date.now() - start) / (end - start)) * 100)));
  }

  ordinalSuffix(n: number): string {
    if (n === 1) return 'st';
    if (n === 2) return 'nd';
    if (n === 3) return 'rd';
    return 'th';
  }

  acceptOffer(): void {
    const offer = this.pendingOffer();
    if (!offer || this.offerActionInProgress()) return;
    this.offerActionInProgress.set(true);
    this.leaseService.acceptOffer(offer.id)
      .pipe(finalize(() => this.offerActionInProgress.set(false)))
      .subscribe({ next: () => { this.pendingOffer.set(null); this.loadData(); } });
  }

  declineOffer(): void {
    const offer = this.pendingOffer();
    if (!offer || this.offerActionInProgress()) return;
    this.offerActionInProgress.set(true);
    this.leaseService.declineOffer(offer.id)
      .pipe(finalize(() => this.offerActionInProgress.set(false)))
      .subscribe({ next: () => this.pendingOffer.set(null) });
  }

  downloadContract(lease: LeaseView): void {
    this.contractLoading.set(true);
    this.leaseService.getContractUrl(lease.id).subscribe({
      next: ({ contractFileUrl }) => {
        window.open(contractFileUrl, '_blank', 'noopener');
        this.contractLoading.set(false);
      },
      error: () => this.contractLoading.set(false),
    });
  }

  toggleExpand(id: string): void {
    this.expandedLeaseId.update(cur => (cur === id ? null : id));
  }

  // ── Renewal ─────────────────────────────────────────────────────────────────

  requestRenewal(lease: LeaseView): void {
    if (this.actionInProgress()) return;
    this.actionInProgress.set(true);
    this.actionError.set(null);
    this.leaseService.requestRenewal(lease.id)
      .pipe(finalize(() => this.actionInProgress.set(false)))
      .subscribe({
        next: () => this.loadData(),
        error: err => this.actionError.set(err?.error?.error ?? 'Could not send renewal request.'),
      });
  }

  // ── Termination (mutual consent) ─────────────────────────────────────────────

  openTerminationForm(): void {
    this.terminationReason = '';
    this.terminationEffectiveDate = '';
    this.actionError.set(null);
    this.showTerminationForm.set(true);
  }

  cancelTerminationForm(): void {
    this.showTerminationForm.set(false);
  }

  submitTermination(lease: LeaseView): void {
    const reason = this.terminationReason.trim();
    if (!reason || this.actionInProgress()) {
      if (!reason) this.actionError.set('Please provide a reason.');
      return;
    }
    this.actionInProgress.set(true);
    this.actionError.set(null);
    this.leaseService.requestTermination(lease.id, reason, this.terminationEffectiveDate || undefined)
      .pipe(finalize(() => this.actionInProgress.set(false)))
      .subscribe({
        next: () => { this.showTerminationForm.set(false); this.loadData(); },
        error: err => this.actionError.set(err?.error?.error ?? 'Could not send termination request.'),
      });
  }

  respondTermination(lease: LeaseView, approve: boolean): void {
    if (this.actionInProgress()) return;
    this.actionInProgress.set(true);
    this.actionError.set(null);
    this.leaseService.respondTermination(lease.id, approve)
      .pipe(finalize(() => this.actionInProgress.set(false)))
      .subscribe({
        next: () => this.loadData(),
        error: err => this.actionError.set(err?.error?.error ?? 'Could not submit your response.'),
      });
  }

  sendChatMessage(): void {
    const text = this.chatInput.trim();
    if (!text || this.chatLoading()) return;

    this.chatMessages.update(msgs => [...msgs, { role: 'user', content: text }]);
    this.chatInput = '';
    this.chatLoading.set(true);

    const lease = this.activeLease();
    this.aiService
      .chatRag({ message: text, leaseId: lease?.id })
      .pipe(finalize(() => this.chatLoading.set(false)))
      .subscribe({
        next: res => this.chatMessages.update(msgs => [...msgs, { role: 'assistant', content: res.message }]),
        error: () =>
          this.chatMessages.update(msgs => [
            ...msgs,
            { role: 'assistant', content: 'Sorry, I could not process your request. Please try again.' },
          ]),
      });
  }
}
