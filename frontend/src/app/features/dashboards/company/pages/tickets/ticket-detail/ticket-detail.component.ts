import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import {
  MaintenanceService,
  TicketDetail,
  TicketPriority,
  ResolutionMethod,
} from '../../../../tenant-dashboard/services/maintenance.service';
import { ToastService } from '../../../../../../shared/components/toast/toast.service';
import { ConfirmModalComponent } from '../../../../../../shared/components/modal/confirm-modal.component';
import { TranslatePipe } from '@ngx-translate/core';

interface TimelineStep {
  key: string;
  labelKey: string;
  icon: string;
  matchStatuses: string[];
}

const STATUS_ORDER: Record<string, number> = {
  Draft: 0,
  Submitted: 1,
  UnderAiReview: 2,
  AiEvaluated: 3,
  WaitingForOwnerDecision: 4,
  WaitingForTenantDecision: 4,
  Disputed: 4,
  AssignedToVendor: 5,
  VendorAccepted: 5,
  InProgress: 6,
  AwaitingPayment: 7,
  Completed: 8,
};

const TIMELINE_STEPS: TimelineStep[] = [
  { key: 'Submitted',  labelKey: 'TICKET_DETAIL.STEP_SUBMITTED',   icon: 'fa-paper-plane',        matchStatuses: ['Submitted'] },
  { key: 'AIReview',   labelKey: 'TICKET_DETAIL.STEP_AI_REVIEW',   icon: 'fa-robot',              matchStatuses: ['UnderAiReview', 'AiEvaluated'] },
  { key: 'Decision',   labelKey: 'TICKET_DETAIL.STEP_DECISION',    icon: 'fa-scale-balanced',     matchStatuses: ['WaitingForOwnerDecision', 'WaitingForTenantDecision', 'Disputed'] },
  { key: 'Assigned',   labelKey: 'TICKET_DETAIL.STEP_ASSIGNED',    icon: 'fa-user-gear',          matchStatuses: ['AssignedToVendor', 'VendorAccepted'] },
  { key: 'InProgress', labelKey: 'TICKET_DETAIL.STEP_IN_PROGRESS', icon: 'fa-screwdriver-wrench', matchStatuses: ['InProgress'] },
  { key: 'Payment',    labelKey: 'TICKET_DETAIL.STEP_PAYMENT',     icon: 'fa-credit-card',        matchStatuses: ['AwaitingPayment'] },
  { key: 'Completed',  labelKey: 'TICKET_DETAIL.STEP_COMPLETED',   icon: 'fa-circle-check',       matchStatuses: ['Completed'] },
];

@Component({
  selector: 'app-company-ticket-detail',
  standalone: true,
  imports: [DatePipe, DecimalPipe, FormsModule, RouterLink, ConfirmModalComponent, TranslatePipe],
  templateUrl: './ticket-detail.component.html',
  styleUrl: './ticket-detail.component.css',
})
export class CompanyTicketDetailComponent implements OnInit {
  private readonly maintenanceService = inject(MaintenanceService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  protected loading = signal(true);
  protected error = signal<string | null>(null);
  protected ticket = signal<TicketDetail | null>(null);

  protected newComment = '';
  protected commentLoading = signal(false);

  protected decisionLoading = signal(false);
  protected pendingDecision = signal<ResolutionMethod | null>(null);

  protected markCompleteLoading = signal(false);

  protected lightboxUrl = signal<string | null>(null);

  protected showDisputeModal = signal(false);
  protected disputeReason = '';
  protected disputeLoading = signal(false);

  protected showPayModal = signal(false);
  protected transactionId = '';
  protected payLoading = signal(false);
  protected walletBalance = signal<number | null>(null);
  protected walletPayLoading = signal(false);

  protected readonly timelineSteps = TIMELINE_STEPS;

  protected readonly currentStepIndex = computed(() => {
    const t = this.ticket();
    if (!t) return -1;
    const statusIdx = STATUS_ORDER[t.status] ?? -1;
    if (statusIdx === -1) return -1;
    return TIMELINE_STEPS.findIndex(step =>
      step.matchStatuses.some(s => STATUS_ORDER[s] === statusIdx)
    );
  });

  protected readonly visibleComments = computed(() =>
    (this.ticket()?.comments ?? []).filter(c => !c.isInternal)
  );

  protected readonly canDecide = computed(() =>
    this.ticket()?.status === 'WaitingForOwnerDecision' && this.ticket()?.resolutionMethod === null
  );

  protected readonly isExternalDecision = computed(() => {
    const m = this.ticket()?.resolutionMethod;
    return m === 1 || m === 'ExternalTechnician';
  });

  protected readonly decisionModalTitle = computed(() =>
    this.pendingDecision() === 0 ? 'TICKET_DETAIL.PLATFORM_MODAL_TITLE' : 'TICKET_DETAIL.EXTERNAL_MODAL_TITLE'
  );

  protected readonly decisionModalMessage = computed(() =>
    this.pendingDecision() === 0
      ? 'TICKET_DETAIL.PLATFORM_MODAL_MSG'
      : 'TICKET_DETAIL.EXTERNAL_MODAL_MSG'
  );

  protected readonly decisionModalLabel = computed(() =>
    this.pendingDecision() === 0 ? 'TICKET_DETAIL.PLATFORM_MODAL_LABEL' : 'TICKET_DETAIL.EXTERNAL_MODAL_LABEL'
  );

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) { this.router.navigate(['/dashboard/company/tickets']); return; }
    this.loadTicket(id);
  }

  private loadTicket(id: string): void {
    this.loading.set(true);
    this.error.set(null);
    this.maintenanceService.getTicketDetail(id)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: t => {
          this.ticket.set(t);
          if (t.status === 'AwaitingPayment') this.loadWalletBalance();
        },
        error: () => this.error.set('Failed to load ticket details.'),
      });
  }

  private loadWalletBalance(): void {
    this.maintenanceService.getWalletBalance().subscribe({
      next: b => this.walletBalance.set(b),
      error: () => this.walletBalance.set(0),
    });
  }

  refresh(): void {
    const id = this.ticket()?.id ?? this.route.snapshot.paramMap.get('id');
    if (id) this.loadTicket(id);
  }

  requestDecision(method: ResolutionMethod): void { this.pendingDecision.set(method); }
  cancelDecisionModal(): void { this.pendingDecision.set(null); }

  confirmDecision(): void {
    const method = this.pendingDecision();
    if (method === null) return;
    const id = this.ticket()?.id;
    if (!id) return;
    this.pendingDecision.set(null);
    this.decisionLoading.set(true);
    this.maintenanceService.makeDecision(id, method)
      .pipe(finalize(() => this.decisionLoading.set(false)))
      .subscribe({
        next: () => { this.toast.success('Decision submitted.'); this.refresh(); },
        error: () => this.toast.error('Failed to submit decision.'),
      });
  }

  markAsComplete(): void {
    const id = this.ticket()?.id;
    if (!id) return;
    this.markCompleteLoading.set(true);
    this.maintenanceService.completeTicket(id)
      .pipe(finalize(() => this.markCompleteLoading.set(false)))
      .subscribe({
        next: () => { this.toast.success('Ticket marked as complete.'); this.refresh(); },
        error: () => this.toast.error('Failed to complete ticket.'),
      });
  }

  addComment(): void {
    const id = this.ticket()?.id;
    if (!id || !this.newComment.trim()) return;
    this.commentLoading.set(true);
    this.maintenanceService.addComment(id, this.newComment.trim())
      .pipe(finalize(() => this.commentLoading.set(false)))
      .subscribe({
        next: comment => {
          this.ticket.update(t => t ? { ...t, comments: [...t.comments, comment] } : t);
          this.newComment = '';
        },
        error: () => this.toast.error('Failed to add comment.'),
      });
  }

  raiseDispute(): void {
    const id = this.ticket()?.id;
    if (!id || !this.disputeReason.trim()) return;
    this.disputeLoading.set(true);
    this.maintenanceService.raiseDispute(id, this.disputeReason.trim())
      .pipe(finalize(() => this.disputeLoading.set(false)))
      .subscribe({
        next: () => {
          this.toast.success('Dispute raised. An admin will review your case.');
          this.showDisputeModal.set(false);
          this.disputeReason = '';
          this.refresh();
        },
        error: () => this.toast.error('Failed to raise dispute. Please try again.'),
      });
  }

  pay(): void {
    const t = this.ticket();
    if (!t?.payment?.id || !this.transactionId.trim()) return;
    this.payLoading.set(true);
    this.maintenanceService.payTicket(t.payment.id, this.transactionId.trim())
      .pipe(finalize(() => this.payLoading.set(false)))
      .subscribe({
        next: () => {
          this.toast.success('Payment submitted successfully.');
          this.showPayModal.set(false);
          this.transactionId = '';
          this.refresh();
        },
        error: () => this.toast.error('Payment failed. Please try again.'),
      });
  }

  payFromWallet(): void {
    const t = this.ticket();
    if (!t?.payment?.id) return;
    this.walletPayLoading.set(true);
    this.maintenanceService.payFromWallet(t.payment.id)
      .pipe(finalize(() => this.walletPayLoading.set(false)))
      .subscribe({
        next: () => { this.toast.success('Paid from wallet. Ticket is now complete.'); this.refresh(); },
        error: (err) => {
          const msg = err?.error?.error ?? 'Wallet payment failed. Please try again.';
          this.toast.error(msg);
        },
      });
  }

  openLightbox(url: string): void { this.lightboxUrl.set(url); }
  closeLightbox(): void { this.lightboxUrl.set(null); }

  priorityClass(p: TicketPriority): string {
    const map: Record<string, string> = { Emergency: 'priority-emergency', Medium: 'priority-medium', Low: 'priority-low' };
    return map[p] ?? '';
  }

  statusClass(s: string): string {
    const map: Record<string, string> = {
      Draft: 'status-draft', Submitted: 'status-submitted',
      UnderAiReview: 'status-ai-review', AiEvaluated: 'status-ai-evaluated',
      WaitingForOwnerDecision: 'status-waiting', WaitingForTenantDecision: 'status-waiting-tenant',
      Disputed: 'status-disputed',
      AssignedToVendor: 'status-assigned', VendorAccepted: 'status-vendor-accepted',
      InProgress: 'status-progress', AwaitingPayment: 'status-awaiting-payment',
      Completed: 'status-done', Cancelled: 'status-cancelled', Rejected: 'status-rejected',
    };
    return map[s] ?? 'status-draft';
  }

  statusLabel(s: string): string {
    const map: Record<string, string> = {
      Draft: 'Draft', Submitted: 'Submitted', UnderAiReview: 'AI Review',
      AiEvaluated: 'Evaluated', WaitingForOwnerDecision: 'Awaiting Decision',
      WaitingForTenantDecision: 'Tenant Decision', Disputed: 'Under Dispute',
      AssignedToVendor: 'Assigned', VendorAccepted: 'Vendor Accepted', InProgress: 'In Progress',
      AwaitingPayment: 'Awaiting Payment', Completed: 'Completed',
      Cancelled: 'Cancelled', Rejected: 'Rejected',
    };
    return map[s] ?? s;
  }

  roleLabel(role: string): string {
    return { Tenant: 'Tenant', Owner: 'Owner', Vendor: 'Vendor', Admin: 'Admin', System: 'System', Company: 'Company' }[role] ?? role;
  }

  stepState(stepIdx: number): 'done' | 'active' | 'pending' {
    const current = this.currentStepIndex();
    if (current === -1) return 'pending';
    if (stepIdx < current) return 'done';
    if (stepIdx === current) return 'active';
    return 'pending';
  }

  isImage(fileType: string): boolean { return fileType?.startsWith('image/') ?? false; }

  timeAgo(dateStr: string): string {
    const s = Math.floor((Date.now() - new Date(dateStr).getTime()) / 1000);
    if (s < 60) return 'just now';
    const m = Math.floor(s / 60);
    if (m < 60) return `${m}m ago`;
    const h = Math.floor(m / 60);
    if (h < 24) return `${h}h ago`;
    return `${Math.floor(h / 24)}d ago`;
  }

  fileSizeLabel(kb: number): string {
    return kb >= 1024 ? `${(kb / 1024).toFixed(1)} MB` : `${kb} KB`;
  }
}
