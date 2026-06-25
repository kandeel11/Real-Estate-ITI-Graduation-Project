/**
 * Public Property Detail
 * Full listing from GET /api/properties/{id}: gallery, specs, amenities,
 * the listing broker/company, and a tenant inquiry form (POST /api/inquiries).
 * Non-tenants see a prompt to sign in instead of the form.
 * The property owner also sees a photo management panel to upload/remove images.
 */

import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { DecimalPipe, DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { PropertyService } from '../../services/property.service';
import { PropertyDetail, PropertyMedia } from '../../models/property.model';
import { AuthService } from '../../../../core/services/auth.service';
import { SavedPropertyService } from '../../../../core/services/saved-property.service';
import {
  RatingService,
  RatingResponse,
} from '../../../dashboards/tenant-dashboard/services/rating.service';
import { StarRatingComponent } from '../../../../shared/components/star-rating/star-rating.component';
import { AgentAvatarComponent } from '../../../../shared/components/agent-avatar/agent-avatar.component';

@Component({
  selector: 'app-property-detail',
  standalone: true,
  imports: [DecimalPipe, DatePipe, RouterLink, FormsModule, TranslatePipe, StarRatingComponent, AgentAvatarComponent],
  templateUrl: './detail.component.html',
  styleUrl: './detail.component.css',
})
export class DetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private propertySvc = inject(PropertyService);
  private ratingSvc = inject(RatingService);
  private auth = inject(AuthService);
  protected savedSvc = inject(SavedPropertyService);

  property = signal<PropertyDetail | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);
  activeImage = signal<string | null>(null);

  isTenant = computed(() => this.auth.userRole() === 'tenant');
  isAuthenticated = this.auth.authenticated;

  /** True when the logged-in user owns this listing. */
  isOwner = computed(() => {
    const p = this.property();
    const me = this.auth.user();
    return !!p && !!me && p.ownerId === me.id;
  });

  // ── Inquiry form ────────────────────────────────────────────────
  message = '';
  sending = signal(false);
  inquirySent = signal(false);
  inquiryError = signal<string | null>(null);
  /** One engagement at a time: null until checked, then whether a new inquiry is allowed. */
  canInquire = signal<boolean | null>(null);
  /** Backend's reason the tenant can't inquire right now (shown when canInquire is false). */
  inquiryBlockReason = signal<string | null>(null);

  // ── Owner photo management ───────────────────────────────────────
  uploadingPhotos = signal(false);
  photoUploadError = signal<string | null>(null);

  // ── Reviews ──────────────────────────────────────────────────────
  reviews = signal<RatingResponse[]>([]);
  reviewsAverage = signal(0);
  reviewsCount = signal(0);
  reviewsLoading = signal(true);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.error.set('Property not found.');
      this.loading.set(false);
      return;
    }
    this.loadProperty(id);
    this.loadReviews(id);
    if (this.isTenant()) this.loadInquiryGate();
  }

  /** Ask the backend whether this tenant may send a new inquiry right now. */
  private loadInquiryGate(): void {
    this.propertySvc.canCreateInquiry().subscribe({
      next: (res) => {
        this.canInquire.set(res.allowed);
        this.inquiryBlockReason.set(res.allowed ? null : res.reason);
      },
      // On error, don't block the UI — the create call still enforces the rule server-side.
      error: () => this.canInquire.set(true),
    });
  }

  private loadReviews(id: string): void {
    this.reviewsLoading.set(true);
    this.ratingSvc.getPropertyRatings(id).subscribe({
      next: (res) => {
        this.reviews.set(res.items);
        this.reviewsAverage.set(res.averageRating);
        this.reviewsCount.set(res.totalCount);
        this.reviewsLoading.set(false);
      },
      error: () => this.reviewsLoading.set(false),
    });
  }

  private loadProperty(id: string): void {
    this.propertySvc.getById(id).subscribe({
      next: (detail) => {
        this.property.set(detail);
        const primary = detail.media.find((m) => m.isPrimary) ?? detail.media[0];
        this.activeImage.set(primary?.mediaUrl ?? null);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('This listing could not be loaded — it may no longer be available.');
        this.loading.set(false);
      },
    });
  }

  activeIndex = computed(() => {
    const p = this.property();
    if (!p) return 0;
    const idx = p.media.findIndex((m) => m.mediaUrl === this.activeImage());
    return idx >= 0 ? idx : 0;
  });

  prevImage(): void {
    const p = this.property();
    if (!p || p.media.length < 2) return;
    const i = this.activeIndex();
    this.activeImage.set(p.media[(i - 1 + p.media.length) % p.media.length].mediaUrl);
  }

  nextImage(): void {
    const p = this.property();
    if (!p || p.media.length < 2) return;
    const i = this.activeIndex();
    this.activeImage.set(p.media[(i + 1) % p.media.length].mediaUrl);
  }

  /** Wishlist toggle for the listing being viewed. */
  toggleSave(): void {
    const p = this.property();
    if (p) this.savedSvc.toggle(p);
  }

  typeIcon(type: string): string {
    switch (type) {
      case 'Villa':
        return 'fa-solid fa-house-chimney';
      case 'House':
        return 'fa-solid fa-house-user';
      default:
        return 'fa-solid fa-building';
    }
  }

  // ── Owner: upload photos ─────────────────────────────────────────
  onOwnerPhotosSelected(event: Event): void {
    const p = this.property();
    if (!p) return;
    const files = Array.from((event.target as HTMLInputElement).files ?? []);
    if (!files.length) return;

    this.uploadingPhotos.set(true);
    this.photoUploadError.set(null);

    const currentCount = p.media.length;
    let pending = files.length;
    let anyError = false;

    files.forEach((file, i) => {
      this.propertySvc.uploadMedia(p.id, file, currentCount + i).subscribe({
        next: (res) => {
          // Auto-set the first photo as primary if there were none before
          if (currentCount === 0 && i === 0) {
            this.propertySvc.setPrimaryMedia(p.id, res.mediaId).subscribe();
          }
          pending--;
          if (pending === 0) {
            this.uploadingPhotos.set(false);
            if (anyError) this.photoUploadError.set('Some photos failed to upload.');
            this.loadProperty(p.id);
          }
        },
        error: () => {
          anyError = true;
          pending--;
          if (pending === 0) {
            this.uploadingPhotos.set(false);
            this.photoUploadError.set('Some photos failed to upload. Please try again.');
          }
        },
      });
    });

    (event.target as HTMLInputElement).value = '';
  }

  // ── Owner: set primary ───────────────────────────────────────────
  setAsPrimary(m: PropertyMedia): void {
    const p = this.property();
    if (!p || m.isPrimary) return;
    this.propertySvc.setPrimaryMedia(p.id, m.id).subscribe({
      next: () => this.loadProperty(p.id),
    });
  }

  // ── Owner: delete photo ──────────────────────────────────────────
  deletePhoto(m: PropertyMedia): void {
    const p = this.property();
    if (!p) return;
    this.propertySvc.deleteMedia(p.id, m.id).subscribe({
      next: () => {
        const updated = p.media.filter((x) => x.id !== m.id);
        this.property.set({ ...p, media: updated });
        const primary = updated.find((x) => x.isPrimary) ?? updated[0];
        this.activeImage.set(primary?.mediaUrl ?? null);
      },
    });
  }

  sendInquiry(): void {
    const p = this.property();
    if (!p || this.sending()) return;
    if (!this.message.trim()) {
      this.inquiryError.set('Please write a message first.');
      return;
    }

    this.sending.set(true);
    this.inquiryError.set(null);

    this.propertySvc.createInquiry(p.id, p.ownerId, p.companyId, this.message.trim()).subscribe({
      next: () => {
        this.sending.set(false);
        this.inquirySent.set(true);
        this.message = '';
      },
      error: (err) => {
        this.sending.set(false);
        // 409 = blocked by the one-engagement-at-a-time gate; switch to the blocked banner.
        if (err?.status === 409 && err?.error?.error) {
          this.canInquire.set(false);
          this.inquiryBlockReason.set(err.error.error);
          return;
        }
        this.inquiryError.set(
          err?.error?.error ?? 'Could not send your inquiry. Please try again.',
        );
      },
    });
  }
}
