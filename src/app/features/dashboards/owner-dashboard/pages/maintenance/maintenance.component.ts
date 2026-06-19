import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { OwnerDataService, MaintenanceTicket } from '../../services/owner-data.service';

type TicketFilter = 'all' | 'open' | 'in_progress' | 'resolved';

@Component({
  selector: 'app-owner-maintenance',
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  templateUrl: './maintenance.component.html',
  styleUrl: './maintenance.component.css',
})
export class MaintenanceComponent implements OnInit {
  private readonly svc = inject(OwnerDataService);

  readonly tickets = signal<MaintenanceTicket[]>([]);
  readonly loading = signal(true);
  readonly filter = signal<TicketFilter>('all');

  readonly filtered = computed(() => {
    const f = this.filter();
    const list = this.tickets();
    if (f === 'all') return list;
    return list.filter((t) => this.bucket(t.status) === f);
  });

  ngOnInit(): void {
    this.svc.getMaintenanceTickets().subscribe({
      next: (t) => { this.tickets.set(t); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  /** Map backend TicketStatus names onto the three UI buckets. */
  bucket(status: string): 'open' | 'in_progress' | 'resolved' {
    const s = (status || '').toLowerCase();
    if (s.includes('progress') || s.includes('assigned') || s.includes('accepted')) return 'in_progress';
    if (s.includes('resolved') || s.includes('completed') || s.includes('closed') || s.includes('done')) return 'resolved';
    return 'open';
  }

  priorityClass(priority: string): string {
    const p = (priority || '').toLowerCase();
    if (p.includes('high') || p.includes('urgent') || p.includes('critical')) return 'prio-high';
    if (p.includes('medium') || p.includes('normal')) return 'prio-medium';
    return 'prio-low';
  }
}
