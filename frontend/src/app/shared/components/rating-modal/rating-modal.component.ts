import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { StarRatingComponent } from '../star-rating/star-rating.component';
import {
  RatingService,
  RatingResponse,
  CreateRatingRequest,
  UpdateRatingRequest,
} from '../../../features/dashboards/tenant-dashboard/services/rating.service';
import { LeaseService } from '../../../features/dashboards/tenant-dashboard/services/lease.service';
import { ToastService } from '../toast/toast.service';

interface PropertyOption {
  id: string;
  title: string;
  leaseId: string | null;
}

@Component({
  selector: 'app-rating-modal',
  standalone: true,
  imports: [ReactiveFormsModule, StarRatingComponent],
  templateUrl: './rating-modal.component.html',
  styleUrl: './rating-modal.component.css',
})
export class RatingModalComponent implements OnChanges {
  private readonly ratingService = inject(RatingService);
  private readonly leaseService = inject(LeaseService);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(FormBuilder);

  @Input() show = false;
  @Input() mode: 'create' | 'edit' = 'create';
  @Input() editRating: RatingResponse | null = null;
  @Input() propertyId: string | null = null;
  @Input() maintenanceCompanyId: string | null = null;
  @Input() leaseId: string | null = null;

  @Output() closed = new EventEmitter<void>();
  @Output() saved = new EventEmitter<RatingResponse>();

  protected starValue = signal(0);
  protected loading = signal(false);
  protected errorMsg = signal<string | null>(null);

  protected propertiesLoading = signal(false);
  protected rentedProperties = signal<PropertyOption[]>([]);
  protected selectedPropertyId = signal('');

  readonly form = this.fb.group({
    reviewText: ['', Validators.maxLength(500)],
  });

  get charCount(): number {
    return this.form.get('reviewText')?.value?.length ?? 0;
  }

  /** True when this is a free-form create with no pre-set target */
  get needsPropertySelection(): boolean {
    return this.mode === 'create' && !this.propertyId && !this.maintenanceCompanyId;
  }

  get isSubmittable(): boolean {
    if (this.starValue() === 0) return false;
    if (this.needsPropertySelection && !this.selectedPropertyId()) return false;
    return true;
  }

  get targetIcon(): string {
    return this.maintenanceCompanyId ? 'fa-wrench' : 'fa-building';
  }

  get targetLabel(): string {
    if (this.maintenanceCompanyId) return 'Maintenance Company';
    if (this.propertyId) return 'Property';
    return '';
  }

  get selectedPropertyTitle(): string {
    return this.rentedProperties().find(p => p.id === this.selectedPropertyId())?.title ?? '';
  }

  private get effectivePropertyId(): string | null {
    return this.propertyId ?? (this.selectedPropertyId() || null);
  }

  private get effectiveLeaseId(): string | null {
    if (this.leaseId) return this.leaseId;
    return this.rentedProperties().find(p => p.id === this.selectedPropertyId())?.leaseId ?? null;
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['show']?.currentValue === true) {
      this.errorMsg.set(null);
      this.selectedPropertyId.set('');

      if (this.mode === 'edit' && this.editRating) {
        this.starValue.set(this.editRating.ratingValue);
        this.form.patchValue({ reviewText: this.editRating.reviewText ?? '' });
      } else {
        this.starValue.set(0);
        this.form.reset({ reviewText: '' });
        if (this.needsPropertySelection && this.rentedProperties().length === 0) {
          this.loadProperties();
        }
      }
    }
  }

  private loadProperties(): void {
    this.propertiesLoading.set(true);
    this.leaseService.getRentedProperties()
      .pipe(finalize(() => this.propertiesLoading.set(false)))
      .subscribe({
        next: items => {
          this.rentedProperties.set(
            items
              .filter(i => i.isActive)
              .map(i => ({ id: i.property.id, title: i.property.title, leaseId: i.leaseId }))
          );
        },
      });
  }

  protected close(): void {
    if (this.loading()) return;
    this.closed.emit();
  }

  protected submit(): void {
    if (!this.isSubmittable) {
      if (this.starValue() === 0) this.errorMsg.set('Please select a star rating.');
      else this.errorMsg.set('Please select a property to rate.');
      return;
    }
    this.errorMsg.set(null);
    this.loading.set(true);
    const reviewText = (this.form.get('reviewText')?.value ?? '').trim() || null;

    if (this.mode === 'create') {
      const body: CreateRatingRequest = {
        leaseId: this.effectiveLeaseId,
        propertyId: this.effectivePropertyId,
        maintenanceCompanyId: this.maintenanceCompanyId,
        ratingValue: this.starValue(),
        reviewText,
      };
      this.ratingService.createRating(body)
        .pipe(finalize(() => this.loading.set(false)))
        .subscribe({
          next: r => {
            this.toast.success('Your review has been submitted and is under review.');
            this.saved.emit(r);
            this.closed.emit();
          },
          error: (err: { status?: number }) => {
            if (err?.status === 409) {
              this.errorMsg.set('You have already submitted a rating for this property.');
            } else if (err?.status === 403) {
              this.errorMsg.set('You can only rate a lease you are part of.');
            } else {
              this.errorMsg.set('Failed to submit rating. Please try again.');
            }
          },
        });
    } else {
      const body: UpdateRatingRequest = {
        ratingValue: this.starValue(),
        reviewText,
      };
      this.ratingService.updateRating(this.editRating!.id, body)
        .pipe(finalize(() => this.loading.set(false)))
        .subscribe({
          next: r => {
            this.toast.success('Your review has been updated.');
            this.saved.emit(r);
            this.closed.emit();
          },
          error: (err: { status?: number; error?: { message?: string } }) => {
            const msg = err?.error?.message ?? '';
            if (msg.toLowerCase().includes('24')) {
              this.errorMsg.set('Ratings can only be edited within 24 hours of submission.');
            } else if (err?.status === 400) {
              this.errorMsg.set('Cannot edit a rating that has already been reviewed.');
            } else {
              this.errorMsg.set('Failed to update rating. Please try again.');
            }
          },
        });
    }
  }
}
