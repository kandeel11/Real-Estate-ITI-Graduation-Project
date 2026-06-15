/**
 * Company Properties Page
 * Lists company properties from the real GET /api/companies/mine/properties
 * endpoint with status filter tabs and title search.
 */

import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { PropertyService } from '../../../../features/properties/services/property.service';
import { Property, PropertyDetail } from '../../../../features/properties/models/property.model';
import { CompanyService } from '../../services/company.service';
import { CompanySubscriptionService } from '../../services/company-subscription.service';
import { Broker } from '../../../../shared/models/company.model';
import { OverLimitBannerComponent } from '../../../../shared/components/over-limit-banner/over-limit-banner.component';

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
  imports: [DecimalPipe, OverLimitBannerComponent, TranslatePipe],
  templateUrl: './properties.component.html',
  styleUrl: './properties.component.css',
})
export class PropertiesComponent implements OnInit {
  private propertySvc = inject(PropertyService);
  private companySvc = inject(CompanyService);
  private subSvc = inject(CompanySubscriptionService);

  properties = signal<Property[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);

  private subscription = toSignal(this.subSvc.getActive());

  /** Numeric property limit (unlimited sentinel while loading → no false banner). */
  propertyMax = computed(() => this.subscription()?.maxProperties ?? 999999);

  /** Plan's property limit — '∞' for unlimited, '…' while loading. */
  maxPropertiesLabel = computed(() => {
    const max = this.subscription()?.maxProperties;
    if (max == null) return '…';
    return max >= 999999 ? '∞' : String(max);
  });

  // ── Broker assignment ───────────────────────────────────────────
  activeBrokers = signal<Broker[]>([]);
  propertyToAssign = signal<Property | null>(null);
  selectedBrokerId = signal<string>('');
  assigning = signal(false);
  assignError = signal<string | null>(null);

  // ── View details ────────────────────────────────────────────────
  viewProperty = signal<Property | null>(null);
  viewDetail = signal<PropertyDetail | null>(null);
  viewLoading = signal(false);

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
    { label: 'COMMON.ALL',     value: 'all',          countKey: 'all' },
    { label: 'COMMON.DRAFT',   value: 'Draft',        countKey: 'draft' },
    { label: 'COMMON.PENDING', value: 'PendingReview', countKey: 'pending' },
    { label: 'COMMON.ACTIVE',  value: 'Active',       countKey: 'active' },
    { label: 'COMMON.RENTED',  value: 'Rented',       countKey: 'rented' },
  ];

  ngOnInit(): void {
    this.load();
    this.companySvc.getBrokers().subscribe({
      next: (brokers) => this.activeBrokers.set(brokers.filter((b) => b.isActive)),
      error: () => this.activeBrokers.set([]),
    });
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

  openView(p: Property): void {
    this.viewProperty.set(p);
    this.viewDetail.set(null);
    this.viewLoading.set(true);
    this.propertySvc.getById(p.id).subscribe({
      next: (d) => { this.viewDetail.set(d); this.viewLoading.set(false); },
      error: () => this.viewLoading.set(false),
    });
  }

  closeView(): void {
    this.viewProperty.set(null);
    this.viewDetail.set(null);
    this.viewLoading.set(false);
  }

  openAssign(property: Property): void {
    this.assignError.set(null);
    this.selectedBrokerId.set(property.listingBrokerId ?? '');
    this.propertyToAssign.set(property);
  }

  onBrokerSelected(event: Event): void {
    this.selectedBrokerId.set((event.target as HTMLSelectElement).value);
  }

  confirmAssign(): void {
    const property = this.propertyToAssign();
    const brokerId = this.selectedBrokerId();
    if (!property || !brokerId || this.assigning()) return;

    this.assigning.set(true);
    this.assignError.set(null);

    this.propertySvc.assignBroker(property.id, brokerId).subscribe({
      next: () => {
        this.assigning.set(false);
        this.propertyToAssign.set(null);
        this.load(); // refresh so listingBrokerName re-enriches
      },
      error: (err) => {
        this.assigning.set(false);
        this.assignError.set(err?.error?.error ?? 'Failed to assign the broker.');
      },
    });
  }
}
