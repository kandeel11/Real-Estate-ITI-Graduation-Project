import { Component, inject, signal, computed, OnInit, OnDestroy, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MaintenanceService } from '../../Services/maintenance';
import { AdminTicket } from '../../Models';
import { DecimalPipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { Subject, Subscription } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';

type Toast = { type: 'success' | 'error'; text: string } | null;

@Component({
  selector: 'app-admin-tickets',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, DecimalPipe, TranslatePipe],
  templateUrl: './tickets.component.html',
  styleUrls: ['./tickets.component.scss'],
})
export class AdminTicketsComponent implements OnInit, OnDestroy {
  private service = inject(MaintenanceService);
  private searchSubject = new Subject<string>();
  private sub = new Subscription();

  tickets = signal<AdminTicket[]>([]);
  loading = signal(false);
  toast = signal<Toast>(null);
  searchQuery = signal('');
  activeFilter = signal('all');

  filteredTickets = computed(() => {
    let list = this.tickets();
    const f = this.activeFilter();
    if (f !== 'all') list = list.filter((t) => t.status === f);
    const q = this.searchQuery().trim().toLowerCase();
    if (q) {
      list = list.filter(
        (t) =>
          t.title.toLowerCase().includes(q) ||
          (t.tenantName ?? '').toLowerCase().includes(q) ||
          (t.companyName ?? '').toLowerCase().includes(q) ||
          (t.categoryName ?? '').toLowerCase().includes(q) ||
          (t.propertyAddress ?? '').toLowerCase().includes(q),
      );
    }
    return list;
  });

  // single O(n) pass instead of N separate filter() calls
  statusCounts = computed(() => {
    const map: Record<string, number> = { all: 0 };
    for (const t of this.tickets()) {
      map['all']++;
      map[t.status] = (map[t.status] ?? 0) + 1;
    }
    return map;
  });

  uniqueStatuses = computed(() => [...new Set(this.tickets().map((t) => t.status))]);

  ngOnInit() {
    this.sub.add(
      this.searchSubject
        .pipe(debounceTime(250), distinctUntilChanged())
        .subscribe((q) => this.searchQuery.set(q)),
    );
    this.load();
  }

  ngOnDestroy() {
    this.sub.unsubscribe();
  }

  load() {
    this.loading.set(true);
    this.service.getAllTickets().subscribe({
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

  showToast(type: 'success' | 'error', text: string) {
    this.toast.set({ type, text });
    setTimeout(() => this.toast.set(null), 3500);
  }

  statusLabel(s: string): string {
    const map: Record<string, string> = {
      Pending: 'Pending',
      WaitingForVendorDecision: 'Awaiting Vendor',
      VendorAccepted: 'Vendor Accepted',
      InProgress: 'In Progress',
      AwaitingPayment: 'Awaiting Payment',
      Completed: 'Completed',
      Rejected: 'Rejected',
      Cancelled: 'Cancelled',
    };
    return map[s] ?? s;
  }

  statusClass(s: string): string {
    const map: Record<string, string> = {
      Pending: 'status--pending',
      WaitingForVendorDecision: 'status--waiting',
      VendorAccepted: 'status--accepted',
      InProgress: 'status--inprogress',
      AwaitingPayment: 'status--awaiting',
      Completed: 'status--completed',
      Rejected: 'status--rejected',
      Cancelled: 'status--cancelled',
    };
    return map[s] ?? '';
  }

  formatDate(iso: string): string {
    if (!iso) return '—';
    return new Date(iso).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  }

  onSearch(event: Event) {
    this.searchSubject.next((event.target as HTMLInputElement).value);
  }
}
