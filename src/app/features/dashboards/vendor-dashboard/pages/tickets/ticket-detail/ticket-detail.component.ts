import {
  Component,
  inject,
  signal,
  OnInit,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TicketsService } from '../../../services/tickets.service';
import { TicketDetailDto } from '../../../models';

type Toast = { type: 'success' | 'error'; text: string } | null;

@Component({
  selector: 'app-ticket-detail',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [],
  templateUrl: './ticket-detail.component.html',
  styleUrls: ['./ticket-detail.component.scss'],
})
export class TicketDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(TicketsService);
  private readonly cdr = inject(ChangeDetectorRef);

  loading = signal(true);
  ticket = signal<TicketDetailDto | null>(null);
  toast = signal<Toast>(null);
  busyId = signal<string | null>(null);

  // Reject modal
  showRejectModal = signal(false);
  rejectReason = signal('');

  // Price modal
  showPriceModal = signal(false);
  priceAmount = signal(0);
  priceCurrency = signal('EGP');

  // Lightbox
  lightboxUrl = signal<string | null>(null);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) this.loadTicket(id);
  }

  private loadTicket(id: string): void {
    this.loading.set(true);
    this.service.getTicketById(id).subscribe({
      next: (t) => {
        this.ticket.set(t);
        this.loading.set(false);
        this.cdr.markForCheck();
      },
      error: () => {
        this.loading.set(false);
        this.showToast('error', 'Failed to load ticket');
        this.cdr.markForCheck();
      },
    });
  }

  goBack(): void {
    this.router.navigate(['/dashboard/maintenance/tickets']);
  }

  // ── ACCEPT ─────────────────────────────────────────────────────────────────
  accept(): void {
    const t = this.ticket();
    const assignmentId = t?.vendorAssignment?.id;
    if (!t || !assignmentId) return;
    this.busyId.set(assignmentId);
    this.service.acceptAssignment(assignmentId).subscribe({
      next: () => {
        this.ticket.update((prev) => prev ? { ...prev, status: 'VendorAccepted' } : prev);
        this.busyId.set(null);
        this.showToast('success', 'Ticket accepted');
        this.cdr.markForCheck();
      },
      error: () => {
        this.busyId.set(null);
        this.showToast('error', 'Failed to accept ticket');
        this.cdr.markForCheck();
      },
    });
  }

  // ── REJECT ─────────────────────────────────────────────────────────────────
  openRejectModal(): void {
    this.rejectReason.set('');
    this.showRejectModal.set(true);
  }

  closeRejectModal(): void {
    this.showRejectModal.set(false);
  }

  confirmReject(): void {
    const t = this.ticket();
    const assignmentId = t?.vendorAssignment?.id;
    if (!t || !assignmentId || !this.rejectReason().trim()) return;
    this.busyId.set(assignmentId);
    this.service.rejectAssignment(assignmentId, this.rejectReason()).subscribe({
      next: () => {
        this.ticket.update((prev) => prev ? { ...prev, status: 'Rejected' } : prev);
        this.busyId.set(null);
        this.closeRejectModal();
        this.showToast('success', 'Ticket rejected');
        this.cdr.markForCheck();
      },
      error: () => {
        this.busyId.set(null);
        this.showToast('error', 'Failed to reject ticket');
        this.cdr.markForCheck();
      },
    });
  }

  // ── START WORK ─────────────────────────────────────────────────────────────
  startWork(): void {
    const t = this.ticket();
    if (!t) return;
    this.busyId.set(t.id);
    this.service.startWork(t.id).subscribe({
      next: () => {
        this.ticket.update((prev) => prev ? { ...prev, status: 'InProgress' } : prev);
        this.busyId.set(null);
        this.showToast('success', 'Work started');
        this.cdr.markForCheck();
      },
      error: () => {
        this.busyId.set(null);
        this.showToast('error', 'Failed to start work');
        this.cdr.markForCheck();
      },
    });
  }

  // ── SET PRICE ──────────────────────────────────────────────────────────────
  openPriceModal(): void {
    const t = this.ticket();
    this.priceAmount.set(t?.payment?.amount ?? 0);
    this.priceCurrency.set(t?.payment?.currency ?? 'EGP');
    this.showPriceModal.set(true);
  }

  closePriceModal(): void {
    this.showPriceModal.set(false);
  }

  confirmPrice(): void {
    const t = this.ticket();
    if (!t || this.priceAmount() <= 0) return;
    this.busyId.set(t.id);
    this.service.setPrice(t.id, this.priceAmount(), this.priceCurrency()).subscribe({
      next: (updated) => {
        this.ticket.set(updated);
        this.busyId.set(null);
        this.closePriceModal();
        this.showToast('success', 'Price set successfully');
        this.cdr.markForCheck();
      },
      error: () => {
        this.busyId.set(null);
        this.showToast('error', 'Failed to set price');
        this.cdr.markForCheck();
      },
    });
  }

  // ── COMPLETE WORK ──────────────────────────────────────────────────────────
  completeWork(): void {
    const t = this.ticket();
    if (!t) return;
    this.busyId.set(t.id);
    this.service.completeWork(t.id).subscribe({
      next: () => {
        this.ticket.update((prev) => prev ? { ...prev, status: 'AwaitingPayment' } : prev);
        this.busyId.set(null);
        this.showToast('success', 'Work completed — awaiting payment');
        this.cdr.markForCheck();
      },
      error: () => {
        this.busyId.set(null);
        this.showToast('error', 'Failed to complete work');
        this.cdr.markForCheck();
      },
    });
  }

  // ── CONFIRM PAYMENT ────────────────────────────────────────────────────────
  confirmPayment(): void {
    const t = this.ticket();
    const payment = t?.payment;
    if (!payment?.transactionId) return;
    this.busyId.set(t!.id);
    this.service.confirmPayment(payment.id, payment.transactionId).subscribe({
      next: () => {
        this.ticket.update((prev) => prev ? { ...prev, status: 'Completed' } : prev);
        this.busyId.set(null);
        this.showToast('success', 'Payment confirmed — ticket completed!');
        this.cdr.markForCheck();
      },
      error: () => {
        this.busyId.set(null);
        this.showToast('error', 'Failed to confirm payment');
        this.cdr.markForCheck();
      },
    });
  }

  // ── Helpers ────────────────────────────────────────────────────────────────
  showToast(type: 'success' | 'error', text: string): void {
    this.toast.set({ type, text });
    setTimeout(() => {
      this.toast.set(null);
      this.cdr.markForCheck();
    }, 3500);
  }

  isBusy(id: string | undefined): boolean {
    if (!id) return false;
    return this.busyId() === id;
  }

  statusLabel(s: string): string {
    return (
      {
        AssignedToVendor: 'Assigned',
        VendorAccepted: 'Accepted',
        InProgress: 'In Progress',
        AwaitingPayment: 'Awaiting Payment',
        Completed: 'Completed',
        Rejected: 'Rejected',
      }[s] ?? s
    );
  }

  statusClass(s: string): string {
    return (
      {
        AssignedToVendor: 'status--pending',
        VendorAccepted: 'status--accepted',
        InProgress: 'status--inprogress',
        AwaitingPayment: 'status--awaiting',
        Completed: 'status--completed',
        Rejected: 'status--rejected',
      }[s] ?? ''
    );
  }

  priorityClass(p: string): string {
    return (
      { Emergency: 'priority--emergency', Medium: 'priority--medium', Low: 'priority--low' }[p] ??
      'priority--low'
    );
  }

  confidencePercent(score: number): number {
    return Math.round(score * 100);
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('en-US', {
      year: 'numeric', month: 'short', day: 'numeric',
    });
  }

  formatDateTime(iso: string): string {
    return new Date(iso).toLocaleString('en-US', {
      month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit',
    });
  }

  formatNum(n: number): string {
    return n.toLocaleString('en-US');
  }

  isImage(fileType: string): boolean {
    return fileType.startsWith('image/');
  }

  openLightbox(url: string): void {
    this.lightboxUrl.set(url);
  }

  closeLightbox(): void {
    this.lightboxUrl.set(null);
  }

  readonly starsArray = [1, 2, 3, 4, 5];
}
