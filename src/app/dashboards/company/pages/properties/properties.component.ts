/**
 * Company Properties Page
 * Lists company properties from the real GET /api/companies/mine/properties
 * endpoint with status filter tabs and title search.
 */

import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { PropertyService } from '../../../../features/properties/services/property.service';
import { Property } from '../../../../features/properties/models/property.model';

type StatusFilter = 'all' | 'Draft' | 'PendingReview' | 'Active' | 'Rented';
type CountKey = 'all' | 'draft' | 'pending' | 'active' | 'rented';

const STATUS_BADGES: Record<string, string> = {
  Draft: 'badge bg-secondary',
  PendingReview: 'badge bg-warning text-dark',
  Active: 'badge bg-success',
  Rented: 'badge bg-primary',
  Rejected: 'badge bg-danger',
  RequiresManualReview: 'badge bg-warning text-dark',
};

@Component({
  selector: 'app-company-properties',
  standalone: true,
  imports: [DecimalPipe],
  templateUrl: './properties.component.html',
  styleUrl: './properties.component.css',
})
export class PropertiesComponent implements OnInit {
  private propertySvc = inject(PropertyService);

  properties = signal<Property[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);

  statusFilter = signal<StatusFilter>('all');
  search = signal('');

  filtered = computed(() => {
    let list = this.properties();
    if (this.statusFilter() !== 'all')
      list = list.filter((p) => p.status === this.statusFilter());
    const q = this.search().toLowerCase();
    if (q) list = list.filter((p) => p.title.toLowerCase().includes(q));
    return list;
  });

  counts = computed(() => ({
    all: this.properties().length,
    draft: this.properties().filter((p) => p.status === 'Draft').length,
    pending: this.properties().filter((p) => p.status === 'PendingReview').length,
    active: this.properties().filter((p) => p.status === 'Active').length,
    rented: this.properties().filter((p) => p.status === 'Rented').length,
  }));

  tabs: { label: string; value: StatusFilter; countKey: CountKey }[] = [
    { label: 'All', value: 'all', countKey: 'all' },
    { label: 'Draft', value: 'Draft', countKey: 'draft' },
    { label: 'Pending', value: 'PendingReview', countKey: 'pending' },
    { label: 'Active', value: 'Active', countKey: 'active' },
    { label: 'Rented', value: 'Rented', countKey: 'rented' },
  ];

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.propertySvc.getCompanyProperties().subscribe({
      next: (list) => {
        this.properties.set(list);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load properties. Is the backend running?');
        this.loading.set(false);
      },
    });
  }

  badgeClass(status: string): string {
    return STATUS_BADGES[status] ?? 'badge bg-secondary';
  }

  statusLabel(status: string): string {
    return status === 'PendingReview' ? 'Pending' : status;
  }

  onSearch(event: Event): void {
    this.search.set((event.target as HTMLInputElement).value);
  }

  // Property editor is separate scope — link target until it's built
  addProperty(): void {
    alert('The property editor is coming soon.');
  }
}
