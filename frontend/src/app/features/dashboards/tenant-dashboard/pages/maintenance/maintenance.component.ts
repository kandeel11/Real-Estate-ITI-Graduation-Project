import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { TranslatePipe } from '@ngx-translate/core';
import {
  MaintenanceService,
  TicketListItem,
  TicketPriority,
} from '../../services/maintenance.service';
import { LeaseService } from '../../services/lease.service';

@Component({
  selector: 'app-maintenance',
  standalone: true,
  imports: [DatePipe, RouterLink, TranslatePipe],
  templateUrl: './maintenance.component.html',
  styleUrl: './maintenance.component.css',
})
export class MaintenanceComponent implements OnInit {
  private readonly maintenanceService = inject(MaintenanceService);
  private readonly leaseService = inject(LeaseService);
  private readonly router = inject(Router);

  /**
   * null  = still checking the active lease
   * true  = maintenance is enabled (company-managed property)
   * false = maintenance is locked (independent broker — no company)
   */
  protected maintenanceEnabled = signal<boolean | null>(null);

  protected loading = signal(false);
  protected error = signal<string | null>(null);
  protected tickets = signal<TicketListItem[]>([]);
  protected total = signal(0);
  protected page = signal(1);
  protected readonly pageSize = 10;
  protected activeFilter = signal('');

  readonly filterTabs = [
    { label: 'COMMON.ALL', value: '' },
    { label: 'COMMON.DRAFT', value: 'Draft' },
    { label: 'MAINTENANCE.SUBMITTED', value: 'Submitted' },
    { label: 'MAINTENANCE.IN_PROGRESS', value: 'InProgress' },
    { label: 'MAINTENANCE.AWAITING_PAYMENT', value: 'AwaitingPayment' },
    { label: 'MAINTENANCE.COMPLETED', value: 'Completed' },
    { label: 'MAINTENANCE.CANCELLED', value: 'Cancelled' },
  ];

  protected readonly totalPages = computed(() => Math.ceil(this.total() / this.pageSize));

  protected readonly pageNumbers = computed(() => {
    const total = this.totalPages();
    const current = this.page();
    const start = Math.max(1, current - 2);
    const end = Math.min(total, current + 2);
    const pages: number[] = [];
    for (let i = start; i <= end; i++) pages.push(i);
    return pages;
  });

  ngOnInit(): void {
    // Check the active lease's maintenance flag before rendering the ticket list.
    // Default to enabled (true) when the field is absent so older leases still work.
    this.leaseService.getActiveLease().subscribe({
      next: lease => {
        const enabled = lease.isMaintenanceEnabled !== false;
        this.maintenanceEnabled.set(enabled);
        if (enabled) this.loadTickets();
      },
      // If there's no active lease the API returns an error — treat as enabled
      // (the maintenance list itself will just be empty).
      error: () => {
        this.maintenanceEnabled.set(true);
        this.loadTickets();
      },
    });
  }

  loadTickets(): void {
    this.loading.set(true);
    this.error.set(null);
    this.maintenanceService
      .getMyTickets(this.activeFilter(), this.page(), this.pageSize)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: result => {
          this.tickets.set(result.items);
          this.total.set(result.total);
        },
        error: () => this.error.set('Failed to load tickets. Please try again.'),
      });
  }

  setFilter(value: string): void {
    this.activeFilter.set(value);
    this.page.set(1);
    this.loadTickets();
  }

  goToPage(p: number): void {
    if (p < 1 || p > this.totalPages()) return;
    this.page.set(p);
    this.loadTickets();
  }

  openDetail(ticket: TicketListItem): void {
    this.router.navigate(['/dashboard/maintenance-requests', ticket.id]);
  }

  priorityClass(p: TicketPriority): string {
    const map: Record<TicketPriority, string> = {
      Emergency: 'priority-emergency',
      Medium: 'priority-medium',
      Low: 'priority-low',
    };
    return map[p] ?? '';
  }

  statusClass(s: string): string {
    const map: Record<string, string> = {
      Draft: 'status-draft',
      Submitted: 'status-submitted',
      UnderAiReview: 'status-ai-review',
      AiEvaluated: 'status-ai-evaluated',
      WaitingForOwnerDecision: 'status-waiting',
      WaitingForTenantDecision: 'status-waiting-tenant',
      AssignedToVendor: 'status-assigned',
      VendorAccepted: 'status-vendor-accepted',
      InProgress: 'status-progress',
      AwaitingPayment: 'status-awaiting-payment',
      Completed: 'status-done',
      Cancelled: 'status-cancelled',
      Rejected: 'status-rejected',
    };
    return map[s] ?? 'status-draft';
  }

  statusLabel(s: string): string {
    const map: Record<string, string> = {
      Draft: 'Draft',
      Submitted: 'Submitted',
      UnderAiReview: 'AI Review',
      AiEvaluated: 'Evaluated',
      WaitingForOwnerDecision: 'Owner Decision',
      WaitingForTenantDecision: 'Your Decision',
      AssignedToVendor: 'Assigned',
      VendorAccepted: 'Vendor Accepted',
      InProgress: 'In Progress',
      AwaitingPayment: 'Awaiting Payment',
      Completed: 'Completed',
      Cancelled: 'Cancelled',
      Rejected: 'Rejected',
    };
    return map[s] ?? s;
  }
}
