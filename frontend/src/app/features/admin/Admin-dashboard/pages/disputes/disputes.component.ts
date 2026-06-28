import { Component, inject, signal, computed, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MaintenanceService } from '../../Services/maintenance';
import { AdminTicket } from '../../Models';

type Toast = { type: 'success' | 'error'; text: string } | null;

@Component({
  selector: 'app-admin-disputes',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule],
  templateUrl: './disputes.component.html',
  styleUrls: ['./disputes.component.scss'],
})
export class AdminDisputesComponent implements OnInit {
  private readonly service = inject(MaintenanceService);
  private readonly router = inject(Router);

  allTickets = signal<AdminTicket[]>([]);
  loading = signal(false);
  toast = signal<Toast>(null);
  searchQuery = signal('');

  disputedTickets = computed(() => {
    const list = this.allTickets().filter(t => t.status === 'Disputed');
    const q = this.searchQuery().trim().toLowerCase();
    if (!q) return list;
    return list.filter(t =>
      t.title.toLowerCase().includes(q) ||
      (t.tenantName ?? '').toLowerCase().includes(q) ||
      (t.propertyAddress ?? '').toLowerCase().includes(q)
    );
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.service.getAllTickets().subscribe({
      next: list => { this.allTickets.set(list); this.loading.set(false); },
      error: () => { this.loading.set(false); this.showToast('error', 'Failed to load disputes'); },
    });
  }

  openDetail(id: string): void {
    this.router.navigate(['/dashboard/admin/disputes', id]);
  }

  showToast(type: 'success' | 'error', text: string): void {
    this.toast.set({ type, text });
    setTimeout(() => this.toast.set(null), 3500);
  }

  formatDate(iso: string): string {
    if (!iso) return '—';
    return new Date(iso).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
  }

  priorityClass(p: string | undefined): string {
    if (!p) return '';
    const map: Record<string, string> = { Emergency: 'priority--emergency', Medium: 'priority--medium', Low: 'priority--low' };
    return map[p] ?? '';
  }
}
