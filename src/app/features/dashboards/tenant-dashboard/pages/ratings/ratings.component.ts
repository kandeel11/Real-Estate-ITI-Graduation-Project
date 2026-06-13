import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { TranslatePipe } from '@ngx-translate/core';
import { CreateRatingDto, Rating, RatingService, RatingTargetType } from '../../services/rating.service';

@Component({
  selector: 'app-ratings',
  standalone: true,
  imports: [DatePipe, ReactiveFormsModule, TranslatePipe],
  templateUrl: './ratings.component.html',
  styleUrl: './ratings.component.css',
})
export class RatingsComponent implements OnInit {
  private readonly ratingService = inject(RatingService);
  private readonly fb = inject(FormBuilder);

  protected activeTab = signal<'reviews' | 'write'>('reviews');
  protected loading = signal(true);
  protected error = signal<string | null>(null);
  protected ratings = signal<Rating[]>([]);
  protected submitLoading = signal(false);
  protected submitSuccess = signal(false);
  protected deleteLoadingId = signal<string | null>(null);

  protected hoveredStar = signal(0);
  protected selectedStar = signal(0);

  readonly writeForm = this.fb.group({
    targetType: ['Property' as RatingTargetType, Validators.required],
    reviewText: ['', [Validators.required, Validators.minLength(10)]],
  });

  readonly starValues = [1, 2, 3, 4, 5];

  ngOnInit(): void {
    this.loadRatings();
  }

  loadRatings(): void {
    this.loading.set(true);
    this.ratingService.getMyRatings().pipe(finalize(() => this.loading.set(false))).subscribe({
      next: data => this.ratings.set(data),
      error: () => this.error.set('Failed to load ratings. Please try again.'),
    });
  }

  setRating(star: number): void {
    this.selectedStar.set(star);
  }

  submitReview(): void {
    if (this.writeForm.invalid || this.selectedStar() === 0) return;
    this.submitLoading.set(true);
    const val = this.writeForm.getRawValue();

    const dto: CreateRatingDto = {
      rating:     this.selectedStar(),
      reviewText: val.reviewText!,
      targetType: val.targetType as RatingTargetType,
      propertyId: val.targetType === 'Property' ? 'prop-001' : undefined,
      maintenanceCompanyId: val.targetType === 'MaintenanceCompany' ? 'mc-001' : undefined,
    };

    this.ratingService.createRating(dto).pipe(finalize(() => this.submitLoading.set(false))).subscribe({
      next: () => {
        this.submitSuccess.set(true);
        this.writeForm.reset({ targetType: 'Property' });
        this.selectedStar.set(0);
        this.loadRatings();
      },
      error: () => this.error.set('Failed to submit review. Please try again.'),
    });
  }

  deleteRating(id: string): void {
    this.deleteLoadingId.set(id);
    this.ratingService.deleteRating(id).pipe(finalize(() => this.deleteLoadingId.set(null))).subscribe({
      next: () => this.ratings.update(cur => cur.filter(r => r.id !== id)),
    });
  }

  isEditable(r: Rating): boolean {
    if (r.status !== 'Pending') return false;
    const ageMs = Date.now() - new Date(r.createdAt).getTime();
    return ageMs < 24 * 60 * 60 * 1000;
  }

  statusClass(s: string): string {
    const map: Record<string, string> = { Pending: 'status-pending', Approved: 'status-approved', Rejected: 'status-rejected' };
    return map[s] ?? '';
  }

  stars(n: number): number[] {
    return Array.from({ length: 5 }, (_, i) => i + 1);
  }
}
