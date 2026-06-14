import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { PropertyService } from '../..//Services/property.service';
import { Property, PropertyStatusString, PropertyDetail, LOCKED_STATUSES } from '../../Models';
import { LanguageService } from '../../../../../core/services/language.service';

type Toast = { type: 'success' | 'error'; text: string } | null;

type VerificationFilter = 'All' | 'Pending' | 'AiVerified' | 'ManuallyVerified' | 'RequiresManualReview' | 'Rejected';

// 🆕 Lifecycle filter — مرتبط بحالة العقار الفعلية
type LifecycleFilter = 'All' | 'Draft' | 'Active' | 'Rented' | 'Inactive';

@Component({
  selector: 'app-portfolio',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './portfolio.component.html',
  styleUrls: ['./portfolio.component.scss'],
})
export class PortfolioComponent {
  private service = inject(PropertyService);
  readonly lang = inject(LanguageService);

  // ===== Data state =====
  properties = signal<Property[]>([]);
  loading = signal(false);
  refreshing = signal(false);
  toast = signal<Toast>(null);
  // ===== Action Lock Logic 🆕 =====
  isLocked(status: PropertyStatusString): boolean {
    return LOCKED_STATUSES.includes(status);
  }

  // lockedReason(status: PropertyStatusString): string {
  //   return (
  //     {
  //       Rented: 'Cannot modify a rented property',
  //       Draft: 'Cannot modify a draft — owner must submit first',
  //     }[status] ?? ''
  //   );
  // }

  // ===== Details Modal 🆕 =====
  openDetailsModal(p: Property) {
    this.closeMenu();
    this.detailProperty.set(null);
    this.loadingDetail.set(true);
    this.activeImageIndex.set(0);

    // Open modal immediately with loading state
    this.detailProperty.set({ ...p } as PropertyDetail);

    this.service.getPropertyById(p.id).subscribe({
      next: (detail) => {
        this.detailProperty.set(detail);
        this.loadingDetail.set(false);
      },
      error: () => {
        this.loadingDetail.set(false);
        this.showToast('error', 'Failed to load property details');
      },
    });
  }

  closeDetailsModal() {
    this.detailProperty.set(null);
    this.loadingDetail.set(false);
    this.activeImageIndex.set(0);
  }

  setActiveImage(i: number) {
    this.activeImageIndex.set(i);
  }

  gateColor(gate: string | undefined): string {
    return { Passed: 'green', Failed: 'red', Pending: 'amber' }[gate ?? ''] ?? 'gray';
  }

  gateIcon(gate: string | undefined): string {
    return (
      {
        Passed: 'fa-check-circle',
        Failed: 'fa-times-circle',
        Pending: 'fa-clock',
      }[gate ?? ''] ?? 'fa-question-circle'
    );
  }

  verificationColor(v: string | undefined): string {
    if (!v) return 'gray';
    if (v.toLowerCase().includes('verified') || v === 'Approved') return 'green';
    if (v === 'Rejected') return 'red';
    if (v === 'Pending') return 'amber';
    return 'blue';
  }
  // ===== Filters =====
  searchQuery = signal('');
  verificationFilter = signal<VerificationFilter>('All');
  lifecycleFilter = signal<LifecycleFilter>('All');
  // ===== Details Modal 🆕 =====
  detailProperty = signal<PropertyDetail | null>(null);
  loadingDetail = signal(false);
  activeImageIndex = signal(0);

  // ===== Pagination 🆕 =====
  currentPage = signal(1);
  pageSize = signal(12);
  pageSizeOptions = [6, 12, 24, 48];

  // ===== Modal / processing =====
  rejectingProperty = signal<Property | null>(null);
  rejectReason = signal('');
  processing = signal(false);
  processingId = signal<string | null>(null);
  openMenuId = signal<string | null>(null);

  // ===== Filter options =====
  verificationOptions: Array<{ key: VerificationFilter; label: string; icon: string }> = [
    { key: 'All', label: 'All', icon: 'fa-list' },
    { key: 'Pending', label: 'Pending', icon: 'fa-clock' },
    { key: 'AiVerified', label: 'AI Verified', icon: 'fa-robot' },
    { key: 'ManuallyVerified', label: 'Verified', icon: 'fa-shield-halved' },
    { key: 'RequiresManualReview', label: 'Needs Review', icon: 'fa-triangle-exclamation' },
    { key: 'Rejected', label: 'Rejected', icon: 'fa-circle-xmark' },
  ];

  lifecycleOptions: Array<{ key: LifecycleFilter; label: string; icon: string }> = [
    { key: 'All', label: 'All', icon: 'fa-list' },
    { key: 'Draft', label: 'Draft', icon: 'fa-pen-to-square' },
    { key: 'Active', label: 'Active', icon: 'fa-circle-check' },
    { key: 'Rented', label: 'Rented', icon: 'fa-key' },
    { key: 'Inactive', label: 'Inactive', icon: 'fa-pause' },
  ];

  // ===== Counts =====
  statusCounts = computed(() => {
    const all = this.properties();
    const counts: Record<string, number> = { All: all.length };
    for (const p of all) counts[p.status] = (counts[p.status] || 0) + 1;
    return counts;
  });

  verificationCounts = computed(() => {
    const all = this.properties();
    const counts: Record<string, number> = { All: all.length };
    for (const p of all) {
      const vs = p.verificationStatus ?? 'Pending';
      counts[vs] = (counts[vs] || 0) + 1;
    }
    return counts;
  });

  // ===== Filtered (before pagination) =====
  filteredProperties = computed(() => {
    let list = this.properties();
    const q = this.searchQuery().toLowerCase().trim();
    const vFilter = this.verificationFilter();
    const lFilter = this.lifecycleFilter();

    // Verification filter — filters by verificationStatus field
    if (vFilter !== 'All') {
      list = list.filter((p) => (p.verificationStatus ?? 'Pending') === vFilter);
    }

    // Lifecycle filter (Draft / Active / Rented / Inactive)
    if (lFilter !== 'All') {
      list = list.filter((p) => p.status === lFilter);
    }

    // Search — matches both title and titleAr so Arabic/English queries work regardless of UI language
    if (q) {
      list = list.filter(
        (p) =>
          p.title?.toLowerCase().includes(q) ||
          p.titleAr?.toLowerCase().includes(q) ||
          p.city?.toLowerCase().includes(q) ||
          p.district?.toLowerCase().includes(q) ||
          p.propertyType?.toLowerCase().includes(q) ||
          p.listingDisplayName?.toLowerCase().includes(q),
      );
    }
    return list;
  });

  // ===== Pagination computeds 🆕 =====
  totalFiltered = computed(() => this.filteredProperties().length);

  totalPages = computed(() => Math.max(1, Math.ceil(this.totalFiltered() / this.pageSize())));

  paginatedProperties = computed(() => {
    const list = this.filteredProperties();
    const page = this.currentPage();
    const size = this.pageSize();
    const start = (page - 1) * size;
    return list.slice(start, start + size);
  });

  pageStart = computed(() =>
    this.totalFiltered() === 0 ? 0 : (this.currentPage() - 1) * this.pageSize() + 1,
  );

  pageEnd = computed(() => Math.min(this.currentPage() * this.pageSize(), this.totalFiltered()));

  // ===== Page numbers to display (with ellipsis) =====
  visiblePages = computed<(number | '...')[]>(() => {
    const total = this.totalPages();
    const current = this.currentPage();
    const pages: (number | '...')[] = [];

    if (total <= 7) {
      for (let i = 1; i <= total; i++) pages.push(i);
      return pages;
    }

    pages.push(1);
    if (current > 3) pages.push('...');

    const start = Math.max(2, current - 1);
    const end = Math.min(total - 1, current + 1);
    for (let i = start; i <= end; i++) pages.push(i);

    if (current < total - 2) pages.push('...');
    pages.push(total);
    return pages;
  });

  constructor() {
    this.loadProperties();
  }

  loadProperties() {
    this.loading.set(true);
    this.service.getAllProperties().subscribe({
      next: (list) => {
        this.properties.set([...list]);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.showToast('error', 'Failed to load properties');
      },
    });
  }

  private silentRefresh() {
    this.refreshing.set(true);
    this.service.getAllProperties().subscribe({
      next: (list) => {
        this.properties.set([...list]);
        this.refreshing.set(false);
      },
      error: () => this.refreshing.set(false),
    });
  }

  // ===== Filter setters (reset page) =====
  onSearchInput(event: Event) {
    this.searchQuery.set((event.target as HTMLInputElement).value);
    this.currentPage.set(1);
  }

  setVerificationFilter(f: VerificationFilter) {
    this.verificationFilter.set(f);
    this.currentPage.set(1);
  }

  setLifecycleFilter(f: LifecycleFilter) {
    this.lifecycleFilter.set(f);
    this.currentPage.set(1);
  }

  clearAllFilters() {
    this.verificationFilter.set('All');
    this.lifecycleFilter.set('All');
    this.searchQuery.set('');
    this.currentPage.set(1);
  }

  hasActiveFilters = computed(
    () =>
      this.verificationFilter() !== 'All' ||
      this.lifecycleFilter() !== 'All' ||
      this.searchQuery().trim().length > 0,
  );

  // ===== Pagination 🆕 =====
  goToPage(page: number | '...') {
    if (page === '...') return;
    const total = this.totalPages();
    const p = Math.max(1, Math.min(total, page));
    this.currentPage.set(p);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  nextPage() {
    if (this.currentPage() < this.totalPages()) this.goToPage(this.currentPage() + 1);
  }

  prevPage() {
    if (this.currentPage() > 1) this.goToPage(this.currentPage() - 1);
  }

  changePageSize(event: Event) {
    const size = parseInt((event.target as HTMLSelectElement).value, 10);
    this.pageSize.set(size);
    this.currentPage.set(1);
  }

  toggleMenu(id: string) {
    this.openMenuId.update((c) => (c === id ? null : id));
  }
  closeMenu() {
    this.openMenuId.set(null);
  }

  // ===== Actions ===== (same as before)
  approve(p: Property) {
    this.closeMenu();
    this.processing.set(true);
    this.processingId.set(p.id);
    this.service.approve(p.id).subscribe({
      next: () => {
        this.updateLocalStatus(p.id, 'Active');
        this.showToast('success', `"${p.title}" is now active 🎉`);
        this.silentRefresh();
        this.processing.set(false);
        this.processingId.set(null);
      },
      error: () => {
        this.processing.set(false);
        this.processingId.set(null);
        this.showToast('error', 'Failed to approve property');
      },
    });
  }

  markInactive(p: Property) {
    this.closeMenu();
    this.processing.set(true);
    this.processingId.set(p.id);
    this.service.markInactive(p.id).subscribe({
      next: () => {
        this.updateLocalStatus(p.id, 'Inactive');
        this.showToast('success', `"${p.title}" set to inactive`);
        this.silentRefresh();
        this.processing.set(false);
        this.processingId.set(null);
      },
      error: () => {
        this.processing.set(false);
        this.processingId.set(null);
        this.showToast('error', 'Failed to update property');
      },
    });
  }

  openRejectModal(p: Property) {
    this.closeMenu();
    this.rejectingProperty.set(p);
    this.rejectReason.set('');
  }
  closeRejectModal() {
    this.rejectingProperty.set(null);
    this.rejectReason.set('');
  }

  confirmReject() {
    const p = this.rejectingProperty();
    const reason = this.rejectReason().trim();
    if (!p) return;
    if (!reason || reason.length < 5) {
      this.showToast('error', 'Please provide a reason (at least 5 characters)');
      return;
    }
    this.processing.set(true);
    this.processingId.set(p.id);
    this.service.reject(p.id, reason).subscribe({
      next: () => {
        this.updateLocalStatus(p.id, 'Rejected');
        this.closeRejectModal();
        this.showToast('success', `"${p.title}" has been rejected`);
        this.silentRefresh();
        this.processing.set(false);
        this.processingId.set(null);
      },
      error: () => {
        this.processing.set(false);
        this.processingId.set(null);
        this.showToast('error', 'Failed to reject property');
      },
    });
  }

  private updateLocalStatus(id: string, status: PropertyStatusString) {
    this.properties.update((list) => list.map((p) => (p.id === id ? { ...p, status } : p)));
  }

  // ===== Helpers =====
  formatPrice(price: number): string {
    if (price >= 1_000_000) return `$${(price / 1_000_000).toFixed(1)}M`;
    if (price >= 1_000) return `$${(price / 1_000).toFixed(0)}K`;
    return `$${price}`;
  }

  formatDate(iso: string): string {
    if (!iso) return '—';
    return new Date(iso).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  }

  statusInfo(s: PropertyStatusString) {
    return {
      Draft: { label: 'Draft', color: 'gray', icon: 'fa-pen-to-square' },
      PendingReview: { label: 'Pending Review', color: 'amber', icon: 'fa-clock' },
      Active: { label: 'Active', color: 'green', icon: 'fa-circle-check' },
      Rented: { label: 'Rented', color: 'blue', icon: 'fa-key' },
      Inactive: { label: 'Inactive', color: 'slate', icon: 'fa-pause' },
      Rejected: { label: 'Rejected', color: 'red', icon: 'fa-circle-xmark' },
    }[s];
  }

  isCardProcessing(id: string): boolean {
    return this.processingId() === id;
  }

  showToast(type: 'success' | 'error', text: string) {
    this.toast.set({ type, text });
    setTimeout(() => this.toast.set(null), 3500);
  }
}
