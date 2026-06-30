import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '../../../../../core/services/auth.service';
import {
  MaintenanceService,
  TicketDetail,
} from '../../../../dashboards/tenant-dashboard/services/maintenance.service';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-company-tickets',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './tickets.component.html',
  styleUrl: './tickets.component.css',
})
export class CompanyTicketsComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly svc = inject(MaintenanceService);
  private readonly router = inject(Router);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly tickets = signal<TicketDetail[]>([]);

  readonly activeFilter = signal<string>('all');

  readonly FILTERS = [
    { value: 'all',                      label: 'All' },
    { value: 'WaitingForOwnerDecision',  label: 'Awaiting Decision' },
    { value: 'AssignedToVendor',         label: 'Assigned' },
    { value: 'InProgress',              label: 'In Progress' },
    { value: 'AwaitingPayment',         label: 'Awaiting Payment' },
    { value: 'Completed',              label: 'Completed' },
  ];

  readonly filtered = computed(() => {
    const f = this.activeFilter();
    const all = this.tickets();
    return f === 'all' ? all : all.filter(t => t.status === f);
  });

  ngOnInit(): void {
    const companyId = this.auth.user()?.companyId;
    if (!companyId) {
      this.error.set('No company ID found. Please log in again.');
      this.loading.set(false);
      return;
    }
    this.load(companyId);
  }

  private load(companyId: string): void {
    this.loading.set(true);
    this.error.set(null);
    this.svc.getRoutedTickets(companyId)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: list => this.tickets.set(list),
        error: () => this.error.set('Failed to load maintenance tickets. Please try again.'),
      });
  }

  reload(): void {
    const id = this.auth.user()?.companyId;
    if (id) this.load(id);
  }

  openTicket(id: string): void {
    this.router.navigate(['/dashboard/company/tickets', id]);
  }

  statusClass(s: string): string {
    const map: Record<string, string> = {
      WaitingForOwnerDecision: 'badge-waiting',
      AssignedToVendor: 'badge-assigned',
      VendorAccepted: 'badge-assigned',
      InProgress: 'badge-progress',
      AwaitingPayment: 'badge-payment',
      Completed: 'badge-done',
      Cancelled: 'badge-cancelled',
    };
    return map[s] ?? 'badge-default';
  }

  statusLabel(s: string): string {
    const map: Record<string, string> = {
      WaitingForOwnerDecision: 'Awaiting Decision',
      AssignedToVendor: 'Assigned',
      VendorAccepted: 'Vendor Accepted',
      InProgress: 'In Progress',
      AwaitingPayment: 'Awaiting Payment',
      Completed: 'Completed',
      Cancelled: 'Cancelled',
    };
    return map[s] ?? s;
  }

  priorityClass(p: string): string {
    return { Emergency: 'priority-emergency', Medium: 'priority-medium', Low: 'priority-low' }[p] ?? '';
  }

  timeAgo(iso: string): string {
    const h = Math.floor((Date.now() - new Date(iso).getTime()) / 3_600_000);
    if (h < 1) return 'just now';
    if (h < 24) return `${h}h ago`;
    const d = Math.floor(h / 24);
    return d < 30 ? `${d}d ago` : `${Math.floor(d / 30)}mo ago`;
  }
}
