import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { finalize } from 'rxjs';
import {
  RatingService,
  RatingResponse,
  ModerationStatus,
} from '../../services/rating.service';
import { LeaseService } from '../../services/lease.service';
import { ToastService } from '../../../../../shared/components/toast/toast.service';
import { StarRatingComponent } from '../../../../../shared/components/star-rating/star-rating.component';
import { RatingModalComponent } from '../../../../../shared/components/rating-modal/rating-modal.component';

@Component({
  selector: 'app-ratings',
  standalone: true,
  imports: [DatePipe, StarRatingComponent, RatingModalComponent],
  templateUrl: './ratings.component.html',
  styleUrl: './ratings.component.css',
})
export class RatingsComponent implements OnInit {
  private readonly ratingService = inject(RatingService);
  private readonly leaseService = inject(LeaseService);
  private readonly toast = inject(ToastService);

  protected loading = signal(true);
  protected error = signal<string | null>(null);
  protected ratings = signal<RatingResponse[]>([]);
  protected deleteLoadingId = signal<string | null>(null);

  protected showModal = signal(false);
  protected modalMode = signal<'create' | 'edit'>('create');
  protected editTarget = signal<RatingResponse | null>(null);
  protected leaseId = signal<string | null>(null);

  ngOnInit(): void {
    this.loadRatings();
    this.leaseService.getActiveLease().subscribe({
      next: lease => this.leaseId.set(lease.id),
    });
  }

  loadRatings(): void {
    this.loading.set(true);
    this.error.set(null);
    this.ratingService.getMyRatings()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: data => this.ratings.set(data),
        error: () => this.error.set('Failed to load your reviews. Please try again.'),
      });
  }

  protected isEditable(r: RatingResponse): boolean {
    if (r.moderationStatus !== 'Pending') return false;
    return Date.now() - new Date(r.createdAt).getTime() < 86_400_000;
  }

  protected isDeletable(r: RatingResponse): boolean {
    return r.moderationStatus === 'Pending';
  }

  protected deleteRating(r: RatingResponse): void {
    this.deleteLoadingId.set(r.id);
    this.ratingService.deleteRating(r.id)
      .pipe(finalize(() => this.deleteLoadingId.set(null)))
      .subscribe({
        next: () => {
          this.ratings.update(cur => cur.filter(x => x.id !== r.id));
          this.toast.success('Review deleted successfully.');
        },
        error: () => this.toast.error('Failed to delete review. Please try again.'),
      });
  }

  protected openCreate(): void {
    this.modalMode.set('create');
    this.editTarget.set(null);
    this.showModal.set(true);
  }

  protected openEdit(r: RatingResponse): void {
    this.modalMode.set('edit');
    this.editTarget.set(r);
    this.showModal.set(true);
  }

  protected onSaved(r: RatingResponse): void {
    if (this.modalMode() === 'create') {
      this.ratings.update(cur => [r, ...cur]);
    } else {
      this.ratings.update(cur => cur.map(x => x.id === r.id ? r : x));
    }
  }

  protected statusBadgeClass(s: ModerationStatus): string {
    const map: Record<ModerationStatus, string> = {
      Pending: 'badge-pending',
      Approved: 'badge-approved',
      Rejected: 'badge-rejected',
    };
    return map[s] ?? '';
  }

  protected statusIcon(s: ModerationStatus): string {
    const map: Record<ModerationStatus, string> = {
      Pending: 'fa-clock',
      Approved: 'fa-check',
      Rejected: 'fa-xmark',
    };
    return map[s] ?? '';
  }

  protected statusLabel(s: ModerationStatus): string {
    return s === 'Pending' ? 'Under Review' : s;
  }

  protected targetLabel(r: RatingResponse): string {
    if (r.propertyId) return 'Property';
    if (r.maintenanceCompanyId) return 'Maintenance Company';
    return 'Unknown';
  }

  protected targetIcon(r: RatingResponse): string {
    return r.propertyId ? 'fa-building' : 'fa-wrench';
  }

  protected targetId(r: RatingResponse): string {
    return r.propertyId ?? r.maintenanceCompanyId ?? '—';
  }

  readonly skeletonRows = [1, 2, 3];
}
