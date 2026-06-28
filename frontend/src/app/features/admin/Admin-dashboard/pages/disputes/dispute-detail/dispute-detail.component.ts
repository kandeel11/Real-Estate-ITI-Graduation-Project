import { Component, inject, signal, computed, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { MaintenanceService, DisputeDetailsDto } from '../../../Services/maintenance';

type Toast = { type: 'success' | 'error'; text: string } | null;

@Component({
  selector: 'app-dispute-detail',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, FormsModule, RouterLink],
  templateUrl: './dispute-detail.component.html',
  styleUrls: ['./dispute-detail.component.scss'],
})
export class DisputeDetailComponent implements OnInit {
  private readonly service = inject(MaintenanceService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  loading = signal(true);
  error = signal<string | null>(null);
  dispute = signal<DisputeDetailsDto | null>(null);
  toast = signal<Toast>(null);

  // Resolve form
  finalResponsibleParty = signal<'Owner' | 'Tenant'>('Owner');
  adminNotes = '';
  resolveLoading = signal(false);
  showResolveForm = signal(false);

  // Lightbox
  lightboxUrl = signal<string | null>(null);

  readonly confidenceColor = computed(() => {
    const score = this.dispute()?.aiEvaluation?.confidenceScore ?? 0;
    if (score >= 80) return '#22c55e';
    if (score >= 50) return '#f59e0b';
    return '#ef4444';
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) { this.router.navigate(['/dashboard/admin/disputes']); return; }
    this.load(id);
  }

  private load(id: string): void {
    this.loading.set(true);
    this.error.set(null);
    this.service.getDisputeDetails(id)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: d => this.dispute.set(d),
        error: () => this.error.set('Failed to load dispute details.'),
      });
  }

  refresh(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) this.load(id);
  }

  resolveDispute(): void {
    const id = this.dispute()?.ticketId;
    if (!id) return;
    this.resolveLoading.set(true);
    this.service.resolveDispute(id, this.finalResponsibleParty(), this.adminNotes.trim() || null)
      .pipe(finalize(() => this.resolveLoading.set(false)))
      .subscribe({
        next: () => {
          this.showToast('success', `Dispute resolved. Final responsible party: ${this.finalResponsibleParty()}.`);
          this.showResolveForm.set(false);
          this.refresh();
        },
        error: () => this.showToast('error', 'Failed to resolve dispute. Please try again.'),
      });
  }

  openLightbox(url: string): void { this.lightboxUrl.set(url); }
  closeLightbox(): void { this.lightboxUrl.set(null); }

  showToast(type: 'success' | 'error', text: string): void {
    this.toast.set({ type, text });
    setTimeout(() => this.toast.set(null), 4000);
  }

  roleLabel(role: string | null): string {
    if (!role) return '—';
    return { tenant: 'Tenant', owner: 'Owner', company_manager: 'Company Manager', admin: 'Admin' }[role.toLowerCase()] ?? role;
  }

  priorityClass(p: string): string {
    return { Emergency: 'priority--emergency', Medium: 'priority--medium', Low: 'priority--low' }[p] ?? '';
  }

  formatCurrency(val: number | null, suffix = 'EGP'): string {
    if (val == null) return '—';
    return `${val.toLocaleString('en-US', { minimumFractionDigits: 2 })} ${suffix}`;
  }

  isImage(fileType: string): boolean {
    return fileType?.startsWith('image/') ?? false;
  }
}
