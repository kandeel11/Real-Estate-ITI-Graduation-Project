import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import {
  MaintenanceService,
  TicketDetail,
  TicketPriority,
  ResolutionMethod,
} from '../../../services/maintenance.service';
import { ToastService } from '../../../../../../shared/components/toast/toast.service';
import { ConfirmModalComponent } from '../../../../../../shared/components/modal/confirm-modal.component';

interface TimelineStep {
  key: string;
  label: string;
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
  AssignedToVendor: 5,
  VendorAccepted: 5,
  InProgress: 6,
  AwaitingPayment: 7,
  Completed: 8,
};

const TIMELINE_STEPS: TimelineStep[] = [
  { key: 'Draft',      label: 'Draft',       icon: 'fa-file-pen',              matchStatuses: ['Draft'] },
  { key: 'Submitted',  label: 'Submitted',   icon: 'fa-paper-plane',           matchStatuses: ['Submitted'] },
  { key: 'AIReview',   label: 'AI Review',   icon: 'fa-robot',                 matchStatuses: ['UnderAiReview', 'AiEvaluated'] },
  { key: 'Decision',   label: 'Decision',    icon: 'fa-scale-balanced',        matchStatuses: ['WaitingForOwnerDecision', 'WaitingForTenantDecision'] },
  { key: 'Assigned',   label: 'Assigned',    icon: 'fa-user-gear',             matchStatuses: ['AssignedToVendor', 'VendorAccepted'] },
  { key: 'InProgress', label: 'In Progress', icon: 'fa-screwdriver-wrench',    matchStatuses: ['InProgress'] },
  { key: 'Payment',    label: 'Payment',     icon: 'fa-credit-card',           matchStatuses: ['AwaitingPayment'] },
  { key: 'Completed',  label: 'Completed',   icon: 'fa-circle-check',          matchStatuses: ['Completed'] },
];

@Component({
  selector: 'app-ticket-detail',
  standalone: true,
  imports: [DatePipe, FormsModule, RouterLink, ConfirmModalComponent],
  templateUrl: './ticket-detail.component.html',
  styleUrl: './ticket-detail.component.css',
})
export class TicketDetailComponent implements OnInit {
  private readonly maintenanceService = inject(MaintenanceService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  protected loading = signal(true);
  protected error = signal<string | null>(null);
  protected ticket = signal<TicketDetail | null>(null);

  // Comments
  protected newComment = '';
  protected commentLoading = signal(false);

  // Decision
  protected decisionLoading = signal(false);
  protected pendingDecision = signal<ResolutionMethod | null>(null);

  // Mark complete (external technician flow)
  protected markCompleteLoading = signal(false);

  // Cancel
  protected cancelLoading = signal(false);
  protected showCancelConfirm = signal(false);

  // Lightbox
  protected lightboxUrl = signal<string | null>(null);

  // Payment modal
  protected showPayModal = signal(false);
  protected transactionId = '';
  protected payLoading = signal(false);

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

  protected readonly isCancellable = computed(() => {
    const s = this.ticket()?.status;
    return s === 'Draft' || s === 'Submitted' || s === 'WaitingForOwnerDecision' || s === 'WaitingForTenantDecision';
  });

  // True once the tenant chose ExternalTechnician
  protected readonly isExternalDecision = computed(() =>
    this.ticket()?.resolutionMethod === 1
  );

  protected readonly decisionModalTitle = computed(() =>
    this.pendingDecision() === 0 ? 'Assign to Platform Vendor?' : 'Use External Technician?'
  );

  protected readonly decisionModalMessage = computed(() =>
    this.pendingDecision() === 0
      ? 'A vetted vendor will be assigned to handle this repair on your behalf. You cannot change this decision after confirming.'
      : 'You will handle this repair with your own contractor. Once confirmed, you can mark the ticket as complete when the work is done.'
  );

  protected readonly decisionModalLabel = computed(() =>
    this.pendingDecision() === 0 ? 'Yes, Assign Vendor' : 'Yes, Use External'
  );

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) { this.router.navigate(['/dashboard/maintenance']); return; }
    this.loadTicket(id);
  }

  private loadTicket(id: string): void {
    this.loading.set(true);
    this.error.set(null);
    this.maintenanceService.getTicketDetail(id)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: t => this.ticket.set(t),
        error: () => this.error.set('Failed to load ticket details.'),
      });
  }

  refresh(): void {
    const id = this.ticket()?.id ?? this.route.snapshot.paramMap.get('id');
    if (id) this.loadTicket(id);
  }

  requestDecision(method: ResolutionMethod): void {
    this.pendingDecision.set(method);
  }

  cancelDecisionModal(): void {
    this.pendingDecision.set(null);
  }

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

  cancelTicket(): void {
    const id = this.ticket()?.id;
    if (!id) return;
    this.cancelLoading.set(true);
    this.showCancelConfirm.set(false);
    this.maintenanceService.cancelTicket(id)
      .pipe(finalize(() => this.cancelLoading.set(false)))
      .subscribe({
        next: () => { this.toast.success('Ticket cancelled.'); this.refresh(); },
        error: () => this.toast.error('Failed to cancel ticket.'),
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

  openLightbox(url: string): void { this.lightboxUrl.set(url); }
  closeLightbox(): void { this.lightboxUrl.set(null); }

  priorityClass(p: TicketPriority): string {
    return { Emergency: 'priority-emergency', Medium: 'priority-medium', Low: 'priority-low' }[p] ?? '';
  }

  statusClass(s: string): string {
    const map: Record<string, string> = {
      Draft: 'status-draft', Submitted: 'status-submitted',
      UnderAiReview: 'status-ai-review', AiEvaluated: 'status-ai-evaluated',
      WaitingForOwnerDecision: 'status-waiting', WaitingForTenantDecision: 'status-waiting-tenant',
      AssignedToVendor: 'status-assigned', VendorAccepted: 'status-vendor-accepted',
      InProgress: 'status-progress', AwaitingPayment: 'status-awaiting-payment',
      Completed: 'status-done', Cancelled: 'status-cancelled', Rejected: 'status-rejected',
    };
    return map[s] ?? 'status-draft';
  }

  statusLabel(s: string): string {
    const map: Record<string, string> = {
      Draft: 'Draft', Submitted: 'Submitted', UnderAiReview: 'AI Review',
      AiEvaluated: 'Evaluated', WaitingForOwnerDecision: 'Owner Decision',
      WaitingForTenantDecision: 'Your Decision', AssignedToVendor: 'Assigned',
      VendorAccepted: 'Vendor Accepted', InProgress: 'In Progress',
      AwaitingPayment: 'Awaiting Payment', Completed: 'Completed',
      Cancelled: 'Cancelled', Rejected: 'Rejected',
    };
    return map[s] ?? s;
  }

  roleLabel(role: string): string {
    const map: Record<string, string> = {
      Tenant: 'Tenant', Owner: 'Owner', Vendor: 'Vendor', Admin: 'Admin', System: 'System',
    };
    return map[role] ?? role;
  }

  stepState(stepIdx: number): 'done' | 'active' | 'pending' {
    const current = this.currentStepIndex();
    if (current === -1) return 'pending';
    if (stepIdx < current) return 'done';
    if (stepIdx === current) return 'active';
    return 'pending';
  }

  isImage(fileType: string): boolean {
    return fileType?.startsWith('image/') ?? false;
  }

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

  formatCurrency(amount: number, currency: string): string {
    try {
      return new Intl.NumberFormat('en-US', { style: 'currency', currency }).format(amount);
    } catch {
      return `${currency} ${amount.toFixed(2)}`;
    }
  }
}
