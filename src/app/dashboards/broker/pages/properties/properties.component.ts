import { Component, OnInit, inject, computed, signal } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../../../core/services/auth.service';
import { BrokerDataService } from '../../services/broker-data.service';
import { Property, PropertyStatus, PropertyDetail } from '../../../../features/properties/models/property.model';
import { PropertyService } from '../../../../features/properties/services/property.service';
import { InquiryDetail } from '../../models/broker.models';
import { InquiriesModalComponent } from '../../components/inquiries-modal/inquiries-modal.component';
import { ChatModalComponent } from '../../components/chat-modal/chat-modal.component';

@Component({
  selector: 'app-broker-properties',
  standalone: true,
  imports: [
    CommonModule, FormsModule, TranslatePipe,
    InquiriesModalComponent, ChatModalComponent,
  ],
  templateUrl: './properties.component.html',
  styleUrl: './properties.component.css',
})
export class PropertiesComponent implements OnInit {
  private readonly auth     = inject(AuthService);
  private readonly svc      = inject(BrokerDataService);
  private readonly propSvc  = inject(PropertyService);
  private readonly router   = inject(Router);

  readonly isCompanyBroker = computed(() => !!this.auth.user()?.companyId);

  readonly properties = signal<Property[]>([]);
  readonly statusFilter = signal<PropertyStatus | 'All'>('All');
  readonly search = signal('');
  readonly loading = signal(true);

  inquiriesProperty = signal<Property | null>(null);
  chatInquiry       = signal<InquiryDetail | null>(null);
  viewProperty      = signal<Property | null>(null);
  viewDetail        = signal<PropertyDetail | null>(null);
  viewLoading       = signal(false);
  /** Incremented whenever we want InquiriesModal to re-fetch. */
  inquiryRefresh    = signal(0);

  readonly statusOptions: { value: PropertyStatus | 'All'; label: string }[] = [
    { value: 'All',           label: 'BROKER.PROPERTIES.FILTER_ALL' },
    { value: 'Active',        label: 'BROKER.PROPERTIES.FILTER_ACTIVE' },
    { value: 'Draft',         label: 'BROKER.PROPERTIES.FILTER_DRAFT' },
    { value: 'PendingReview', label: 'BROKER.PROPERTIES.FILTER_PENDING' },
    { value: 'Rented',        label: 'BROKER.PROPERTIES.FILTER_RENTED' },
  ];

  readonly filtered = computed(() => {
    const status = this.statusFilter();
    const q = this.search().toLowerCase();
    return this.properties().filter(p => {
      const matchStatus = status === 'All' || p.status === status;
      const matchSearch = !q
        || p.title.toLowerCase().includes(q)
        || p.city.toLowerCase().includes(q)
        || p.district.toLowerCase().includes(q);
      return matchStatus && matchSearch;
    });
  });

  ngOnInit(): void {
    this.svc.getProperties().subscribe({
      next: props => { this.properties.set(props); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  addProperty():    void { this.router.navigate(['/properties/new']); }
  resumeProperty(p: Property): void {
    this.router.navigate(['/properties/new'], { queryParams: { propertyId: p.id } });
  }

  openView(p: Property): void {
    this.viewProperty.set(p);
    this.viewDetail.set(null);
    this.viewLoading.set(true);
    this.propSvc.getById(p.id).subscribe({
      next: d => { this.viewDetail.set(d); this.viewLoading.set(false); },
      error: () => this.viewLoading.set(false),
    });
  }
  closeView(): void {
    this.viewProperty.set(null);
    this.viewDetail.set(null);
    this.viewLoading.set(false);
  }

  openInquiries(p: Property): void  { this.inquiriesProperty.set(p); }
  closeInquiries(): void            { this.inquiriesProperty.set(null); this.chatInquiry.set(null); }

  openChat(inq: InquiryDetail): void { this.chatInquiry.set(inq); }

  closeChat(): void {
    this.chatInquiry.set(null);
    // Bump the refresh counter so InquiriesModal re-fetches
    this.inquiryRefresh.update(n => n + 1);
  }

  onLeadConverted(): void {
    // Close both modals — the broker should now go to Leads page
    this.chatInquiry.set(null);
    this.inquiriesProperty.set(null);
    this.router.navigate(['/dashboard/broker/leads']);
  }

  countByStatus(s: PropertyStatus): number {
    return this.properties().filter(p => p.status === s).length;
  }

  statusClass(s: PropertyStatus): string {
    const m: Record<string, string> = {
      Active: 'badge-active', Draft: 'badge-draft', PendingReview: 'badge-pending',
      Rented: 'badge-rented', Rejected: 'badge-rejected', RequiresManualReview: 'badge-pending',
    };
    return m[s] ?? 'badge-draft';
  }

  statusLabel(s: PropertyStatus): string {
    const m: Record<string, string> = {
      Active: 'BROKER.PROPERTIES.STATUS_ACTIVE',
      Draft: 'BROKER.PROPERTIES.STATUS_DRAFT',
      PendingReview: 'BROKER.PROPERTIES.STATUS_PENDING',
      Rented: 'BROKER.PROPERTIES.STATUS_RENTED',
      Rejected: 'BROKER.PROPERTIES.STATUS_REJECTED',
      RequiresManualReview: 'BROKER.PROPERTIES.STATUS_PENDING',
    };
    return m[s] ?? 'BROKER.PROPERTIES.STATUS_DRAFT';
  }
}
