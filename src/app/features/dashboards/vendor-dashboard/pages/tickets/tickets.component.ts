import {
  Component,
  inject,
  signal,
  computed,
  OnInit,
  ChangeDetectionStrategy,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TicketsService } from '../../services/tickets.service';
import { AssignedTicket, AssignmentStatus } from '../../models';
import { TranslatePipe } from '@ngx-translate/core';
import { DecimalPipe } from '@angular/common';

type Toast = { type: 'success' | 'error'; text: string } | null;
type FilterStatus = 'all' | AssignmentStatus;

@Component({
  selector: 'app-maintenance-tickets',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, DecimalPipe, TranslatePipe],
  templateUrl: './tickets.component.html',
  styleUrls: ['./tickets.component.scss'],
})
export class MaintenanceTicketsComponent implements OnInit {
  private service = inject(TicketsService);

  tickets = signal<AssignedTicket[]>([]);
  loading = signal(false);
  toast = signal<Toast>(null);
  activeFilter = signal<FilterStatus>('all');

  // Reject modal
  showRejectModal = signal(false);
  rejectingTicket = signal<AssignedTicket | null>(null);
  rejectReason = signal('');

  // Set price modal
  showPriceModal = signal(false);
  pricingTicket = signal<AssignedTicket | null>(null);
  priceAmount = signal(0);
  priceCurrency = signal('EGP');

  busyId = signal<string | null>(null);

  filteredTickets = computed(() => {
    const f = this.activeFilter();
    if (f === 'all') return this.tickets();
    // 'Pending' tab covers both Pending and WaitingForVendorDecision
    if (f === 'Pending')
      return this.tickets().filter(
        (t) => t.status === 'Pending' || t.status === 'WaitingForVendorDecision',
      );
    return this.tickets().filter((t) => t.status === f);
  });

  // single O(n) pass; WaitingForVendorDecision counts under Pending
  filterCounts = computed(() => {
    const counts = {
      all: 0,
      Pending: 0,
      VendorAccepted: 0,
      InProgress: 0,
      AwaitingPayment: 0,
      Completed: 0,
    };
    for (const t of this.tickets()) {
      counts.all++;
      if (t.status === 'Pending' || t.status === 'WaitingForVendorDecision') counts.Pending++;
      else if (t.status === 'VendorAccepted') counts.VendorAccepted++;
      else if (t.status === 'InProgress') counts.InProgress++;
      else if (t.status === 'AwaitingPayment') counts.AwaitingPayment++;
      else if (t.status === 'Completed') counts.Completed++;
    }
    return counts;
  });

  ngOnInit() {
    this.load();
  }

  load() {
    this.loading.set(true);
    this.service.getMyAssignments().subscribe({
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
  accept(ticket: AssignedTicket) {
    this.busyId.set(ticket.assignmentId);
    this.service.acceptAssignment(ticket.assignmentId).subscribe({
      next: () => {
        this.updateStatus(ticket.assignmentId, 'VendorAccepted');
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
  openRejectModal(ticket: AssignedTicket) {
    this.rejectingTicket.set(ticket);
    this.rejectReason.set('');
    this.showRejectModal.set(true);
  }

  closeRejectModal() {
    this.showRejectModal.set(false);
    this.rejectingTicket.set(null);
  }

  confirmReject() {
    const ticket = this.rejectingTicket();
    if (!ticket) return;
    this.busyId.set(ticket.assignmentId);
    this.service.rejectAssignment(ticket.assignmentId, this.rejectReason()).subscribe({
      next: () => {
        this.tickets.update((list) => list.filter((t) => t.assignmentId !== ticket.assignmentId));
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
  startWork(ticket: AssignedTicket) {
    this.busyId.set(ticket.id);
    this.service.startWork(ticket.id).subscribe({
      next: () => {
        this.updateStatusById(ticket.id, 'InProgress');
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
  openPriceModal(ticket: AssignedTicket) {
    this.pricingTicket.set(ticket);
    this.priceAmount.set(ticket.amount ?? 0);
    this.priceCurrency.set(ticket.currency ?? 'EGP');
    this.showPriceModal.set(true);
  }

  closePriceModal() {
    this.showPriceModal.set(false);
    this.pricingTicket.set(null);
  }

  confirmPrice() {
    const ticket = this.pricingTicket();
    if (!ticket || this.priceAmount() <= 0) return;
    this.busyId.set(ticket.id);
    this.service.setPrice(ticket.id, this.priceAmount(), this.priceCurrency()).subscribe({
      next: () => {
        this.tickets.update((list) =>
          list.map((t) =>
            t.id === ticket.id
              ? { ...t, amount: this.priceAmount(), currency: this.priceCurrency() }
              : t,
          ),
        );
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
  completeWork(ticket: AssignedTicket) {
    if (!confirm('Mark this ticket as complete? Status will change to Awaiting Payment.')) return;
    this.busyId.set(ticket.id);
    this.service.completeWork(ticket.id).subscribe({
      next: () => {
        this.updateStatusById(ticket.id, 'AwaitingPayment');
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
  confirmPayment(ticket: AssignedTicket) {
    if (!confirm('Confirm that payment has been received?')) return;
    this.busyId.set(ticket.id);
    this.service.getPayments(ticket.id).subscribe({
      next: (payments) => {
        const pending = payments.find((p) => !p.transactionId);
        if (!pending) {
          this.busyId.set(null);
          this.showToast('error', 'No pending payment found');
          return;
        }
        this.service.confirmPayment(pending.id).subscribe({
          next: () => {
            this.updateStatusById(ticket.id, 'Completed');
            this.busyId.set(null);
            this.showToast('success', 'Payment confirmed — ticket completed!');
          },
          error: () => {
            this.busyId.set(null);
            this.showToast('error', 'Failed to confirm payment');
          },
        });
      },
      error: () => {
        this.busyId.set(null);
        this.showToast('error', 'Failed to fetch payments');
      },
    });
  }

  // ── Helpers ────────────────────────────────────────────────────────────────
  private updateStatus(assignmentId: string, status: AssignmentStatus) {
    this.tickets.update((list) =>
      list.map((t) => (t.assignmentId === assignmentId ? { ...t, status } : t)),
    );
  }

  private updateStatusById(ticketId: string, status: AssignmentStatus) {
    this.tickets.update((list) => list.map((t) => (t.id === ticketId ? { ...t, status } : t)));
  }

  showToast(type: 'success' | 'error', text: string) {
    this.toast.set({ type, text });
    setTimeout(() => this.toast.set(null), 3500);
  }

  statusLabel(s: string): string {
    const map: Record<string, string> = {
      Pending: 'Pending',
      VendorAccepted: 'Accepted',
      InProgress: 'In Progress',
      AwaitingPayment: 'Awaiting Payment',
      Completed: 'Completed',
      Rejected: 'Rejected',
      WaitingForVendorDecision: 'Awaiting Decision',
    };
    return map[s] ?? s;
  }

  statusClass(s: string): string {
    const map: Record<string, string> = {
      Pending: 'status--pending',
      VendorAccepted: 'status--accepted',
      InProgress: 'status--inprogress',
      AwaitingPayment: 'status--awaiting',
      Completed: 'status--completed',
      Rejected: 'status--rejected',
    };
    return map[s] ?? '';
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  }

  isBusy(id: string): boolean {
    return this.busyId() === id;
  }
}
