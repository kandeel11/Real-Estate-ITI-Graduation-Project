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
import { RatingService, RatingResponse } from '../../../services/rating.service';
import { ToastService } from '../../../../../../shared/components/toast/toast.service';
import { ConfirmModalComponent } from '../../../../../../shared/components/modal/confirm-modal.component';
import { StarRatingComponent } from '../../../../../../shared/components/star-rating/star-rating.component';
import { RatingModalComponent } from '../../../../../../shared/components/rating-modal/rating-modal.component';
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
  { key: 'Draft',      labelKey: 'TICKET_DETAIL.STEP_DRAFT',       icon: 'fa-file-pen',              matchStatuses: ['Draft'] },
  { key: 'Submitted',  labelKey: 'TICKET_DETAIL.STEP_SUBMITTED',   icon: 'fa-paper-plane',           matchStatuses: ['Submitted'] },
  { key: 'AIReview',   labelKey: 'TICKET_DETAIL.STEP_AI_REVIEW',   icon: 'fa-robot',                 matchStatuses: ['UnderAiReview', 'AiEvaluated'] },
  { key: 'Decision',   labelKey: 'TICKET_DETAIL.STEP_DECISION',    icon: 'fa-scale-balanced',        matchStatuses: ['WaitingForOwnerDecision', 'WaitingForTenantDecision', 'Disputed'] },
  { key: 'Assigned',   labelKey: 'TICKET_DETAIL.STEP_ASSIGNED',    icon: 'fa-user-gear',             matchStatuses: ['AssignedToVendor', 'VendorAccepted'] },
  { key: 'InProgress', labelKey: 'TICKET_DETAIL.STEP_IN_PROGRESS', icon: 'fa-screwdriver-wrench',    matchStatuses: ['InProgress'] },
  { key: 'Payment',    labelKey: 'TICKET_DETAIL.STEP_PAYMENT',     icon: 'fa-credit-card',           matchStatuses: ['AwaitingPayment'] },
  { key: 'Completed',  labelKey: 'TICKET_DETAIL.STEP_COMPLETED',   icon: 'fa-circle-check',          matchStatuses: ['Completed'] },
];

@Component({
  selector: 'app-ticket-detail',
  standalone: true,
  imports: [DatePipe, FormsModule, RouterLink, ConfirmModalComponent, StarRatingComponent, RatingModalComponent, TranslatePipe],
  templateUrl: './ticket-detail.component.html',
  styleUrl: './ticket-detail.component.css',
})
export class TicketDetailComponent implements OnInit {
  private readonly maintenanceService = inject(MaintenanceService);
  private readonly ratingService = inject(RatingService);
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

  // Stripe Checkout redirect for maintenance payment
  protected payRedirectLoading = signal(false);

  // Dispute
  protected showDisputeModal = signal(false);
  protected disputeReason = '';
  protected disputeLoading = signal(false);

  // Vendor rating
  protected vendorRating = signal<RatingResponse | null>(null);
  protected vendorRatingLoading = signal(false);
  protected showVendorRatingModal = signal(false);
  protected vendorRatingModalMode = signal<'create' | 'edit'>('create');

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

  protected readonly canDispute = computed(() =>
    this.ticket()?.status === 'WaitingForTenantDecision'
  );

  // True once the tenant chose ExternalTechnician (API may return int 1 or string)
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
    if (!id) { this.router.navigate(['/dashboard/maintenance-requests']); return; }
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
          if (t.status === 'Completed' && t.vendorAssignment?.vendorId) {
            this.loadVendorRating(t.vendorAssignment.vendorId);
          }
        },
        error: () => this.error.set('Failed to load ticket details.'),
      });
  }

  private loadVendorRating(vendorId: string): void {
    this.vendorRatingLoading.set(true);
    this.ratingService.getMyRatings()
      .pipe(finalize(() => this.vendorRatingLoading.set(false)))
      .subscribe({
        next: ratings => {
          const match = ratings.find(r => r.maintenanceCompanyId === vendorId) ?? null;
          this.vendorRating.set(match);
        },
      });
  }

  protected isVendorRatingEditable(): boolean {
    const r = this.vendorRating();
    if (!r || r.moderationStatus !== 'Pending') return false;
    return Date.now() - new Date(r.createdAt).getTime() < 86_400_000;
  }

  protected openVendorRatingModal(mode: 'create' | 'edit'): void {
    this.vendorRatingModalMode.set(mode);
    this.showVendorRatingModal.set(true);
  }

  protected onVendorRatingSaved(r: RatingResponse): void {
    this.vendorRating.set(r);
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

  payViaCheckout(): void {
    const id = this.ticket()?.id;
    if (!id || this.payRedirectLoading()) return;
    this.payRedirectLoading.set(true);
    this.maintenanceService.createMaintenanceCheckout(id)
      .pipe(finalize(() => this.payRedirectLoading.set(false)))
      .subscribe({
        next: ({ checkoutUrl }) => { window.location.href = checkoutUrl; },
        error: () => this.toast.error('Could not start payment. Please try again.'),
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
      AiEvaluated: 'Evaluated', WaitingForOwnerDecision: 'Owner Decision',
      WaitingForTenantDecision: 'Your Decision', Disputed: 'Under Dispute',
      AssignedToVendor: 'Assigned', VendorAccepted: 'Vendor Accepted', InProgress: 'In Progress',
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
