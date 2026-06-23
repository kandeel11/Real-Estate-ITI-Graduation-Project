import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { finalize } from 'rxjs';
import { BrokerDataService, BrokerLease } from '../../services/broker-data.service';
import { LeaseUploadModalComponent } from '../../components/lease-upload-modal/lease-upload-modal.component';
import { Lead } from '../../models/broker.models';

type LeaseFilter = 'all' | 'active' | 'ended' | 'pending';

@Component({
  selector: 'app-broker-leases',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, TranslatePipe, LeaseUploadModalComponent],
  templateUrl: './leases.component.html',
  styleUrl: './leases.component.css',
})
export class LeasesComponent implements OnInit {
  private readonly svc = inject(BrokerDataService);

  readonly leases = signal<BrokerLease[]>([]);
  readonly loading = signal(true);
  readonly filter = signal<LeaseFilter>('all');

  // ── Renewal / termination ──────────────────────────────────────────────────
  readonly renewLease = signal<BrokerLease | null>(null);
  readonly renewLead = signal<Lead | null>(null);
  readonly terminationLease = signal<BrokerLease | null>(null);
  terminationReason = '';
  terminationEffectiveDate = '';
  readonly actionInProgress = signal(false);
  readonly actionError = signal<string | null>(null);

  // Contract download — id of the lease whose PDF link is being fetched.
  readonly contractLoadingId = signal<string | null>(null);

  readonly filtered = computed(() => {
    const f = this.filter();
    const list = this.leases();
    if (f === 'all') return list;
    if (f === 'pending') return list.filter(l => l.tenantStatus === 'pending_tenant');
    if (f === 'ended') return list.filter(l => {
      const s = this.computedStatus(l);
      return s === 'ended' || s === 'terminated';
    });
    return list.filter(l => this.computedStatus(l) === f);
  });

  ngOnInit(): void {
    this.refresh();
  }

  refresh(): void {
    this.loading.set(true);
    this.svc.getLeases().subscribe({
      next: l => { this.leases.set(l); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  // ── Renewal ─────────────────────────────────────────────────────────────────

  openRenewal(lease: BrokerLease): void {
    // The renewal upload targets the lease directly; the synthetic lead only
    // feeds the modal's display + email prefill (its id is unused in renew mode).
    this.renewLead.set({
      id: lease.id,
      propertyId: lease.propertyId,
      propertyTitle: lease.propertyTitle,
      tenantName: lease.tenantName,
      tenantEmail: lease.tenantEmail,
      status: 'closed_won',
      stageEnteredAt: Date.now(),
    } as Lead);
    this.renewLease.set(lease);
  }

  closeRenewal(): void {
    this.renewLease.set(null);
    this.renewLead.set(null);
  }

  onRenewalCreated(): void {
    this.closeRenewal();
    this.refresh();
  }

  // ── Termination (mutual consent) ─────────────────────────────────────────────

  openTermination(lease: BrokerLease): void {
    this.terminationReason = '';
    this.terminationEffectiveDate = '';
    this.actionError.set(null);
    this.terminationLease.set(lease);
  }

  closeTermination(): void {
    this.terminationLease.set(null);
  }

  submitTermination(): void {
    const lease = this.terminationLease();
    const reason = this.terminationReason.trim();
    if (!lease || this.actionInProgress()) return;
    if (!reason) { this.actionError.set('Please provide a reason.'); return; }
    this.actionInProgress.set(true);
    this.actionError.set(null);
    this.svc.requestTermination(lease.id, reason, this.terminationEffectiveDate || undefined)
      .pipe(finalize(() => this.actionInProgress.set(false)))
      .subscribe({
        next: () => { this.closeTermination(); this.refresh(); },
        error: err => this.actionError.set(err?.error?.error ?? 'Could not send termination request.'),
      });
  }

  respondTermination(lease: BrokerLease, approve: boolean): void {
    if (this.actionInProgress()) return;
    this.actionInProgress.set(true);
    this.actionError.set(null);
    this.svc.respondTermination(lease.id, approve)
      .pipe(finalize(() => this.actionInProgress.set(false)))
      .subscribe({
        next: () => this.refresh(),
        error: () => { this.actionInProgress.set(false); this.refresh(); },
      });
  }

  computedStatus(lease: BrokerLease): 'active' | 'ended' | 'expiring' | 'terminated' {
    // Lifecycle wins: a terminated/ended lease is never "active", even with no end date.
    if (lease.lifecycleStatus === 'terminated') return 'terminated';
    if (lease.lifecycleStatus === 'ended') return 'ended';
    const end = new Date(lease.endDate);
    if (isNaN(end.getTime())) return 'active';
    const now = new Date();
    const daysLeft = (end.getTime() - now.getTime()) / 86400000;
    if (end < now) return 'ended';
    if (daysLeft <= 30) return 'expiring';
    return 'active';
  }

  /** Fetch the lease's contract URL and download the PDF (no tab/preview). */
  openContract(lease: BrokerLease): void {
    if (this.contractLoadingId()) return;
    this.contractLoadingId.set(lease.id);
    this.svc
      .getContractUrl(lease.id)
      .pipe(finalize(() => this.contractLoadingId.set(null)))
      .subscribe({
        next: ({ contractFileUrl }) => this.triggerDownload(contractFileUrl),
        error: () => this.actionError.set('Could not download the contract. Please try again.'),
      });
  }

  /** Force a file download (the SAS URL is served with Content-Disposition: attachment). */
  private triggerDownload(url: string): void {
    const a = document.createElement('a');
    a.href = url;
    a.download = 'lease-contract.pdf';
    a.rel = 'noopener';
    document.body.appendChild(a);
    a.click();
    a.remove();
  }
}
