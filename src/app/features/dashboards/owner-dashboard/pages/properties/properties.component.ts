import { Component, OnInit, inject, computed, signal } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../../../../core/services/auth.service';
import { OwnerDataService } from '../../services/owner-data.service';
import { Property, PropertyStatus, PropertyDetail } from '../../../../../features/properties/models/property.model';
import { PropertyService } from '../../../../../features/properties/services/property.service';
import { InquiryDetail } from '../../models/owner.models';
import { InquiriesModalComponent } from '../../components/inquiries-modal/inquiries-modal.component';
import { ChatModalComponent } from '../../components/chat-modal/chat-modal.component';
import { ManageImagesModalComponent } from '../../components/manage-images-modal/manage-images-modal.component';
import { ConfirmModalComponent } from '../../../../../shared/components/modal/confirm-modal.component';
import { ToastService } from '../../../../../shared/components/toast/toast.service';

@Component({
  selector: 'app-owner-properties',
  standalone: true,
  imports: [
    CommonModule, FormsModule, TranslatePipe,
    InquiriesModalComponent, ChatModalComponent,
    ManageImagesModalComponent, ConfirmModalComponent,
  ],
  templateUrl: './properties.component.html',
  styleUrl: './properties.component.css',
})
export class PropertiesComponent implements OnInit {
  private readonly auth     = inject(AuthService);
  private readonly svc      = inject(OwnerDataService);
  private readonly propSvc  = inject(PropertyService);
  private readonly router   = inject(Router);
  private readonly toast    = inject(ToastService);

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

  imagesProperty    = signal<Property | null>(null);
  propertyToDelete  = signal<Property | null>(null);
  deleting          = signal(false);
  /** Id of the property whose row action (submit / feature) is in flight. */
  busyId            = signal<string | null>(null);

  readonly statusOptions: { value: PropertyStatus | 'All'; label: string }[] = [
    { value: 'All',           label: 'OWNER.PROPERTIES.FILTER_ALL' },
    { value: 'Active',        label: 'OWNER.PROPERTIES.FILTER_ACTIVE' },
    { value: 'Draft',         label: 'OWNER.PROPERTIES.FILTER_DRAFT' },
    { value: 'PendingReview', label: 'OWNER.PROPERTIES.FILTER_PENDING' },
    { value: 'Rented',        label: 'OWNER.PROPERTIES.FILTER_RENTED' },
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
    this.reload();
  }

  private reload(): void {
    this.svc.getProperties().subscribe({
      next: props => { this.properties.set(props); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  addProperty():    void { this.router.navigate(['/properties/new']); }
  resumeProperty(p: Property): void {
    this.router.navigate(['/properties/new'], { queryParams: { propertyId: p.id } });
  }

  // ── Manage images ────────────────────────────────────────────────────────────
  openImages(p: Property): void  { this.imagesProperty.set(p); }
  closeImages(): void            { this.imagesProperty.set(null); }
  onImagesChanged(): void        { this.reload(); }

  // ── Submit a draft for review ────────────────────────────────────────────────
  submitDraft(p: Property): void {
    if (this.busyId()) return;
    this.busyId.set(p.id);
    this.propSvc.submitForReview(p.id).subscribe({
      next: () => { this.busyId.set(null); this.toast.success('Submitted for review.'); this.reload(); },
      error: (err) => { this.busyId.set(null); this.toast.error(err?.error?.error || 'Failed to submit the listing.'); },
    });
  }

  // ── Make / remove featured (backend enforces the plan limit) ──────────────────
  toggleFeatured(p: Property): void {
    if (this.busyId()) return;
    this.busyId.set(p.id);
    const next = !p.isFeatured;
    this.propSvc.setFeatured(p.id, next).subscribe({
      next: () => {
        this.busyId.set(null);
        this.toast.success(next ? 'Listing featured.' : 'Listing unfeatured.');
        this.reload();
      },
      error: (err) => { this.busyId.set(null); this.toast.error(err?.error?.error || 'Could not update featured status.'); },
    });
  }

  // ── Delete (confirm modal) ────────────────────────────────────────────────────
  readonly deleteMessage = computed(() => {
    const p = this.propertyToDelete();
    return p
      ? `Delete "${p.title}"? Its photos are removed and the plan slot is freed. This cannot be undone.`
      : '';
  });

  confirmDelete(): void {
    const p = this.propertyToDelete();
    if (!p || this.deleting()) return;
    this.deleting.set(true);
    this.propSvc.delete(p.id).subscribe({
      next: () => { this.deleting.set(false); this.propertyToDelete.set(null); this.toast.success('Listing deleted.'); this.reload(); },
      error: (err) => { this.deleting.set(false); this.propertyToDelete.set(null); this.toast.error(err?.error?.error || 'Failed to delete the listing.'); },
    });
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
    this.router.navigate(['/dashboard/owner/leads']);
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
      Active: 'OWNER.PROPERTIES.STATUS_ACTIVE',
      Draft: 'OWNER.PROPERTIES.STATUS_DRAFT',
      PendingReview: 'OWNER.PROPERTIES.STATUS_PENDING',
      Rented: 'OWNER.PROPERTIES.STATUS_RENTED',
      Rejected: 'OWNER.PROPERTIES.STATUS_REJECTED',
      RequiresManualReview: 'OWNER.PROPERTIES.STATUS_PENDING',
    };
    return m[s] ?? 'OWNER.PROPERTIES.STATUS_DRAFT';
  }
}
