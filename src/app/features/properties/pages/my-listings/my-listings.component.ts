/**
 * My Listings (owners + brokers)
 * Every property the caller created, in all statuses — the home for draft
 * recovery: Continue setup reopens the wizard at step 2, Submit sends a
 * draft to review, Delete frees the plan slot. Active listings can be
 * marked as rented.
 */

import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { DecimalPipe, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { PropertyService } from '../../services/property.service';
import { Property } from '../../models/property.model';
import { ConfirmModalComponent } from '../../../../shared/components/modal/confirm-modal.component';
import { CompanySubscriptionService } from '../../../../dashboards/company/services/company-subscription.service';

const STATUS_BADGES: Record<string, string> = {
  Draft: 'badge bg-secondary',
  PendingReview: 'badge bg-warning text-dark',
  Active: 'badge bg-success',
  Rented: 'badge bg-primary',
  Rejected: 'badge bg-danger',
  RequiresManualReview: 'badge bg-warning text-dark',
};

@Component({
  selector: 'app-my-listings',
  standalone: true,
  imports: [DecimalPipe, DatePipe, RouterLink, ConfirmModalComponent],
  templateUrl: './my-listings.component.html',
  styleUrl: './my-listings.component.css',
})
export class MyListingsComponent implements OnInit {
  private propertySvc = inject(PropertyService);
  private subSvc = inject(CompanySubscriptionService);

  properties = signal<Property[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);
  actionError = signal<string | null>(null);
  busyId = signal<string | null>(null);
  subscription = signal<{ hasFeaturedListings?: boolean } | null>(null);

  draftCount = computed(
    () => this.properties().filter((p) => p.status === 'Draft').length
  );

  canFeature = computed(() => this.subscription()?.hasFeaturedListings === true);
  featuredId = computed(() => this.properties().find((p) => p.isFeatured)?.id ?? null);

  ngOnInit(): void {
    this.load();
    this.subSvc.getActive().subscribe((sub) => this.subscription.set(sub));
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.propertySvc.getMine().subscribe({
      next: (list) => {
        this.properties.set(
          [...list].sort((a, b) => b.createdAt.localeCompare(a.createdAt))
        );
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load your listings. Is the backend running?');
        this.loading.set(false);
      },
    });
  }

  badgeClass(status: string): string {
    return STATUS_BADGES[status] ?? 'badge bg-secondary';
  }

  statusLabel(status: string): string {
    switch (status) {
      case 'PendingReview':
        return 'Pending Review';
      case 'RequiresManualReview':
        return 'Manual Review';
      default:
        return status;
    }
  }

  submit(property: Property): void {
    if (this.busyId()) return;
    this.busyId.set(property.id);
    this.actionError.set(null);
    this.propertySvc.submitForReview(property.id).subscribe({
      next: () => {
        this.busyId.set(null);
        this.load();
      },
      error: (err) => {
        this.busyId.set(null);
        this.actionError.set(err?.error?.error ?? 'Failed to submit the listing.');
      },
    });
  }

  markRented(property: Property): void {
    if (this.busyId()) return;
    this.busyId.set(property.id);
    this.actionError.set(null);
    this.propertySvc.markAsRented(property.id).subscribe({
      next: () => {
        this.busyId.set(null);
        this.load();
      },
      error: (err) => {
        this.busyId.set(null);
        this.actionError.set(err?.error?.error ?? 'Failed to mark the listing as rented.');
      },
    });
  }

  toggleFeatured(property: Property): void {
    if (this.busyId()) return;
    this.busyId.set(property.id);
    this.actionError.set(null);
    const next = !property.isFeatured;
    this.propertySvc.setFeatured(property.id, next).subscribe({
      next: () => {
        this.busyId.set(null);
        this.load();
      },
      error: (err) => {
        this.busyId.set(null);
        this.actionError.set(err?.error?.error ?? 'Failed to update featured status.');
      },
    });
  }

  // ── Delete (confirm modal) ──────────────────────────────────────
  propertyToDelete = signal<Property | null>(null);
  deleting = signal(false);

  deleteMessage = computed(() => {
    const p = this.propertyToDelete();
    return p
      ? `Delete "${p.title}"? Its photos are removed and the plan slot is freed. This cannot be undone.`
      : '';
  });

  confirmDelete(): void {
    const property = this.propertyToDelete();
    if (!property || this.deleting()) return;
    this.deleting.set(true);
    this.actionError.set(null);
    this.propertySvc.delete(property.id).subscribe({
      next: () => {
        this.deleting.set(false);
        this.propertyToDelete.set(null);
        this.load();
      },
      error: (err) => {
        this.deleting.set(false);
        this.propertyToDelete.set(null);
        this.actionError.set(err?.error?.error ?? 'Failed to delete the listing.');
      },
    });
  }
}
