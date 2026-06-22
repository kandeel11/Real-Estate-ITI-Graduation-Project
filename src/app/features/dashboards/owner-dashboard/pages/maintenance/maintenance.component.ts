import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { OwnerDataService, MaintenanceTicket } from '../../services/owner-data.service';

@Component({
  selector: 'app-owner-maintenance',
  standalone: true,
  imports: [DatePipe],
  templateUrl: './maintenance.component.html',
  styleUrl: './maintenance.component.css',
})
export class MaintenanceComponent implements OnInit {
  private readonly ownerDataService = inject(OwnerDataService);
  private readonly router = inject(Router);

  protected loading = signal(true);
  protected error = signal<string | null>(null);
  protected tickets = signal<MaintenanceTicket[]>([]);
  protected total = signal(0);
  protected page = signal(1);
  protected readonly pageSize = 10;
  protected activeFilter = signal('');

  readonly filterTabs = [
    { label: 'All',              value: '' },
    { label: 'Decision Needed',  value: 'WaitingForOwnerDecision' },
    { label: 'In Progress',      value: 'InProgress' },
    { label: 'Awaiting Payment', value: 'AwaitingPayment' },
    { label: 'Completed',        value: 'Completed' },
    { label: 'Cancelled',        value: 'Cancelled' },
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
    this.loadTickets();
  }

  loadTickets(): void {
    this.loading.set(true);
    this.error.set(null);
    this.ownerDataService
      .getMaintenanceTickets(this.activeFilter(), this.page(), this.pageSize)
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

  openDetail(ticket: MaintenanceTicket): void {
    this.router.navigate(['/dashboard/owner/maintenance', ticket.id]);
  }

  needsDecision(ticket: MaintenanceTicket): boolean {
    return ticket.status === 'WaitingForOwnerDecision';
  }

  priorityClass(p: string): string {
    const map: Record<string, string> = {
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
      WaitingForOwnerDecision: 'Your Decision',
      WaitingForTenantDecision: 'Tenant Decision',
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
