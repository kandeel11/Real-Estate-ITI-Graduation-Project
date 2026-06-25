import { Component, inject, signal, computed, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { Router } from '@angular/router';
import {
  MaintenanceCompanyProfile,
  MaintenanceProfileService,
} from '../services/maintenance-profile.service';
import { TicketsService } from '../services/tickets.service';
import { TicketDetailDto } from '../models';

type Toast = { type: 'success' | 'error'; text: string } | null;

@Component({
  selector: 'app-maintenance-overview',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [],
  templateUrl: './overview.component.html',
  styleUrls: ['./overview.component.scss'],
})
export class OverviewComponent implements OnInit {
  private readonly profileService = inject(MaintenanceProfileService);
  private readonly ticketsService = inject(TicketsService);
  private readonly router = inject(Router);

  profileLoading = signal(true);
  ticketsLoading = signal(true);
  profile = signal<MaintenanceCompanyProfile | null>(null);
  tickets = signal<TicketDetailDto[]>([]);

  toast = signal<Toast>(null);
  busyId = signal<string | null>(null);

  // Reject modal
  showRejectModal = signal(false);
  rejectingTicket = signal<TicketDetailDto | null>(null);
  rejectReason = signal('');

  // Set Price modal
  showPriceModal = signal(false);
  pricingTicket = signal<TicketDetailDto | null>(null);
  priceAmount = signal(0);
  priceCurrency = signal('EGP');

  /** Tickets that need the vendor to take action */
  pendingTickets = computed(() =>
    this.tickets().filter((t) =>
      ['AssignedToVendor', 'VendorAccepted', 'InProgress', 'AwaitingPayment'].includes(t.status),
    ),
  );

  ngOnInit(): void {
    this.loadProfile();
    this.loadTickets();
  }

  loadAll(): void {
    this.loadProfile();
    this.loadTickets();
  }

  private loadProfile(): void {
    this.profileLoading.set(true);
    this.profileService.getProfile().subscribe({
      next: (p) => {
        this.profile.set(p);
        this.profileLoading.set(false);
      },
      error: () => this.profileLoading.set(false),
    });
  }

  private loadTickets(): void {
    this.ticketsLoading.set(true);
    this.ticketsService.getMyAssignments().subscribe({
      next: (list) => {
        this.tickets.set(list);
        this.ticketsLoading.set(false);
      },
      error: () => {
        this.ticketsLoading.set(false);
        this.showToast('error', 'Failed to load tickets');
      },
    });
  }

  // ── ACCEPT ─────────────────────────────────────────────────────────────────
  accept(ticket: TicketDetailDto): void {
    const assignmentId = ticket.vendorAssignment?.id;
    if (!assignmentId) return;
    this.busyId.set(assignmentId);
    this.ticketsService.acceptAssignment(assignmentId).subscribe({
      next: () => {
        this.updateStatus(ticket.id, 'VendorAccepted');
        this.busyId.set(null);
        this.showToast('success', 'Ticket accepted — ready to start work');
      },
      error: () => {
        this.busyId.set(null);
        this.showToast('error', 'Failed to accept ticket');
      },
    });
  }

  // ── REJECT ─────────────────────────────────────────────────────────────────
  openRejectModal(ticket: TicketDetailDto): void {
    this.rejectingTicket.set(ticket);
    this.rejectReason.set('');
    this.showRejectModal.set(true);
  }

  closeRejectModal(): void {
    this.showRejectModal.set(false);
    this.rejectingTicket.set(null);
  }

  confirmReject(): void {
    const ticket = this.rejectingTicket();
    const assignmentId = ticket?.vendorAssignment?.id;
    if (!ticket || !assignmentId || !this.rejectReason().trim()) return;
    this.busyId.set(assignmentId);
    this.ticketsService.rejectAssignment(assignmentId, this.rejectReason()).subscribe({
      next: () => {
        this.tickets.update((list) => list.filter((t) => t.id !== ticket.id));
        this.busyId.set(null);
        this.closeRejectModal();
        this.showToast('success', 'Ticket rejected');
      },
      error: () => {
        this.busyId.set(null);
        this.showToast('error', 'Failed to reject ticket');
      },
    });
  }

  // ── START WORK ─────────────────────────────────────────────────────────────
  startWork(ticket: TicketDetailDto): void {
    this.busyId.set(ticket.id);
    this.ticketsService.startWork(ticket.id).subscribe({
      next: () => {
        this.updateStatus(ticket.id, 'InProgress');
        this.busyId.set(null);
        this.showToast('success', 'Work started — status is now In Progress');
      },
      error: () => {
        this.busyId.set(null);
        this.showToast('error', 'Failed to start work');
      },
    });
  }

  // ── SET PRICE ──────────────────────────────────────────────────────────────
  openPriceModal(ticket: TicketDetailDto): void {
    this.pricingTicket.set(ticket);
    this.priceAmount.set(ticket.payment?.amount ?? 0);
    this.priceCurrency.set(ticket.payment?.currency ?? 'EGP');
    this.showPriceModal.set(true);
  }

  closePriceModal(): void {
    this.showPriceModal.set(false);
    this.pricingTicket.set(null);
  }

  confirmPrice(): void {
    const ticket = this.pricingTicket();
    if (!ticket || this.priceAmount() <= 0) return;
    this.busyId.set(ticket.id);
    this.ticketsService.setPrice(ticket.id, this.priceAmount(), this.priceCurrency()).subscribe({
      next: (updated) => {
        this.tickets.update((list) => list.map((t) => (t.id === ticket.id ? updated : t)));
        this.busyId.set(null);
        this.closePriceModal();
        this.showToast('success', 'Price set successfully');
      },
      error: () => {
        this.busyId.set(null);
        this.showToast('error', 'Failed to set price');
      },
    });
  }

  // ── COMPLETE WORK ──────────────────────────────────────────────────────────
  completeWork(ticket: TicketDetailDto): void {
    this.busyId.set(ticket.id);
    this.ticketsService.completeWork(ticket.id).subscribe({
      next: () => {
        this.updateStatus(ticket.id, 'AwaitingPayment');
        this.busyId.set(null);
        this.showToast('success', 'Work completed — awaiting payment');
      },
      error: () => {
        this.busyId.set(null);
        this.showToast('error', 'Failed to complete work');
      },
    });
  }

  // ── CONFIRM PAYMENT ────────────────────────────────────────────────────────
  confirmPayment(ticket: TicketDetailDto): void {
    const payment = ticket.payment;
    if (!payment?.transactionId) return;
    this.busyId.set(ticket.id);
    this.ticketsService.confirmPayment(payment.id, payment.transactionId).subscribe({
      next: () => {
        this.tickets.update((list) => list.filter((t) => t.id !== ticket.id));
        this.busyId.set(null);
        this.showToast('success', 'Payment confirmed — ticket completed!');
      },
      error: () => {
        this.busyId.set(null);
        this.showToast('error', 'Failed to confirm payment');
      },
    });
  }

  // ── Helpers ────────────────────────────────────────────────────────────────
  private updateStatus(ticketId: string, status: string): void {
    this.tickets.update((list) =>
      list.map((t) => (t.id === ticketId ? { ...t, status } : t)),
    );
  }

  showToast(type: 'success' | 'error', text: string): void {
    this.toast.set({ type, text });
    setTimeout(() => this.toast.set(null), 3500);
  }

  isBusy(id: string | undefined): boolean {
    if (!id) return false;
    return this.busyId() === id;
  }

  formatNum(n: number, decimals = 0): string {
    return n.toLocaleString('en-US', {
      minimumFractionDigits: decimals,
      maximumFractionDigits: decimals,
    });
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  }

  priorityClass(p: string): string {
    return (
      { Emergency: 'priority--emergency', Medium: 'priority--medium', Low: 'priority--low' }[p] ??
      'priority--low'
    );
  }

  priorityLabel(p: string): string {
    return p;
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

  viewTickets(): void {
    this.router.navigate(['/dashboard/maintenance/tickets']);
  }

  viewDetail(id: string): void {
    this.router.navigate(['/dashboard/maintenance/tickets', id]);
  }

  readonly starsArray = [1, 2, 3, 4, 5];
  readonly skeletonArray = [1, 2, 3, 4];
}
