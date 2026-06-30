import {
  Component,
  inject,
  signal,
  computed,
  OnInit,
  ChangeDetectionStrategy,
} from '@angular/core';
import { Router } from '@angular/router';
import { TicketsService } from '../../services/tickets.service';
import { TicketDetailDto } from '../../models';

type Toast = { type: 'success' | 'error'; text: string } | null;
type FilterTab = 'all' | 'active' | 'completed' | 'rejected';

@Component({
  selector: 'app-maintenance-tickets',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [],
  templateUrl: './tickets.component.html',
  styleUrls: ['./tickets.component.scss'],
})
export class MaintenanceTicketsComponent implements OnInit {
  private readonly service = inject(TicketsService);
  private readonly router = inject(Router);

  tickets = signal<TicketDetailDto[]>([]);
  loading = signal(false);
  toast = signal<Toast>(null);
  activeFilter = signal<FilterTab>('all');

  // Reject modal
  showRejectModal = signal(false);
  rejectingTicket = signal<TicketDetailDto | null>(null);
  rejectReason = signal('');

  // Set price modal
  showPriceModal = signal(false);
  pricingTicket = signal<TicketDetailDto | null>(null);
  priceAmount = signal(0);
  priceCurrency = signal('EGP');

  busyId = signal<string | null>(null);

  readonly totalEarned = computed(() =>
    this.tickets()
      .filter((t) => t.status === 'Completed' && t.payment?.status === 'Paid')
      .reduce((sum, t) => sum + (t.payment?.amount ?? 0), 0),
  );

  readonly pendingRevenue = computed(() =>
    this.tickets()
      .filter((t) => t.payment && t.payment.status !== 'Paid')
      .reduce((sum, t) => sum + (t.payment?.amount ?? 0), 0),
  );

  readonly currency = computed(() =>
    this.tickets().find((t) => t.payment?.currency)?.payment?.currency ?? 'EGP',
  );

  filteredTickets = computed(() => {
    const f = this.activeFilter();
    const list = this.tickets();
    if (f === 'all') return list;
    if (f === 'active')
      return list.filter((t) =>
        ['AssignedToVendor', 'VendorAccepted', 'InProgress', 'AwaitingPayment'].includes(t.status),
      );
    if (f === 'completed') return list.filter((t) => t.status === 'Completed');
    if (f === 'rejected') return list.filter((t) => t.status === 'Rejected');
    return list;
  });

  filterCounts = computed(() => {
    const counts = { all: 0, active: 0, completed: 0, rejected: 0 };
    for (const t of this.tickets()) {
      counts.all++;
      if (['AssignedToVendor', 'VendorAccepted', 'InProgress', 'AwaitingPayment'].includes(t.status))
        counts.active++;
      else if (t.status === 'Completed') counts.completed++;
      else if (t.status === 'Rejected') counts.rejected++;
    }
    return counts;
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.service.getVendorDashboard().subscribe({
      next: (list) => {
        this.tickets.set(list);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.showToast('error', 'Failed to load tickets');
      },
    });
  }

  // ── ACCEPT ─────────────────────────────────────────────────────────────────
  accept(ticket: TicketDetailDto): void {
    const assignmentId = ticket.vendorAssignment?.id;
    if (!assignmentId) return;
    this.busyId.set(assignmentId);
    this.service.acceptAssignment(assignmentId).subscribe({
      next: () => {
        this.updateStatus(ticket.id, 'VendorAccepted');
        this.busyId.set(null);
        this.showToast('success', 'Ticket accepted successfully');
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
    this.service.rejectAssignment(assignmentId, this.rejectReason()).subscribe({
      next: () => {
        this.updateStatus(ticket.id, 'Rejected');
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
    this.service.startWork(ticket.id).subscribe({
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
    this.service.setPrice(ticket.id, this.priceAmount(), this.priceCurrency()).subscribe({
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
    this.service.completeWork(ticket.id).subscribe({
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
    this.service.confirmPayment(payment.id, payment.transactionId).subscribe({
      next: () => {
        this.updateStatus(ticket.id, 'Completed');
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

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  }

  formatNum(n: number, decimals = 0): string {
    return n.toLocaleString('en-US', {
      minimumFractionDigits: decimals,
      maximumFractionDigits: decimals,
    });
  }

  viewDetail(id: string): void {
    this.router.navigate(['/dashboard/maintenance/tickets', id]);
  }
}
