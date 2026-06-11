/**
 * Property Creation Wizard (owners + brokers)
 *
 * Step 1 — details. Owners must also provide their full name, national ID
 *          image, and title deed (ownership proof); the backend fires the AI
 *          ownership check on create. Brokers skip ownership entirely.
 *          Next = POST /api/properties (multipart) → { id }.
 * Step 2 — photos, amenities, and AI description: generate → poll the draft →
 *          edit/accept EN + AR texts → PATCH /api/AI/{generationId}/accept.
 *          Submit = POST /api/properties/{id}/submit → AI duplicate check.
 * Step 3 — confirmation.
 */

import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../../core/services/auth.service';
import { PropertyService } from '../../services/property.service';
import { Amenity, DescriptionDraft } from '../../models/property.model';

interface PhotoUpload {
  name: string;
  preview: string;
  status: 'uploading' | 'done' | 'error';
}

const MAX_POLL_ATTEMPTS = 20;
const POLL_INTERVAL_MS = 3000;

@Component({
  selector: 'app-create-property',
  standalone: true,
  imports: [ReactiveFormsModule, FormsModule, RouterLink],
  templateUrl: './create-property.component.html',
  styleUrl: './create-property.component.css',
})
export class CreatePropertyComponent implements OnInit {
  private fb = inject(FormBuilder);
  private propertySvc = inject(PropertyService);
  private auth = inject(AuthService);

  step = signal<1 | 2 | 3>(1);
  isOwner = computed(() => this.auth.userRole() === 'owner');

  // ── Step 1: details ─────────────────────────────────────────────
  creating = signal(false);
  createError = signal<string | null>(null);
  propertyId = signal<string | null>(null);
  nationalIdFile: File | null = null;
  ownershipProofFile: File | null = null;
  filesError = signal<string | null>(null);

  form = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.minLength(5)]],
    titleAr: ['', Validators.required],
    propertyType: ['Apartment', Validators.required],
    price: [0, [Validators.required, Validators.min(1)]],
    bedrooms: [1, [Validators.required, Validators.min(0)]],
    bathrooms: [1, [Validators.required, Validators.min(0)]],
    areaSqm: [50, [Validators.required, Validators.min(1)]],
    floorNumber: [0, Validators.min(0)],
    totalFloors: [1, Validators.min(1)],
    yearBuilt: [2020, [Validators.min(1900), Validators.max(2030)]],
    isFurnished: [false],
    petsAllowed: [false],
    address: ['', Validators.required],
    apartmentNumber: [null as number | null],
    country: ['Egypt', Validators.required],
    governorate: ['', Validators.required],
    city: ['', Validators.required],
    district: ['', Validators.required],
    ownerFullName: [''],
  });

  // ── Step 2: photos / amenities / description ────────────────────
  photos = signal<PhotoUpload[]>([]);
  amenities = signal<Amenity[]>([]);
  selectedAmenities = signal<Set<string>>(new Set());

  generating = signal(false);
  aiError = signal<string | null>(null);
  draft = signal<DescriptionDraft | null>(null);
  textEn = '';
  textAr = '';
  accepted = signal(false);
  accepting = signal(false);

  submitting = signal(false);
  submitError = signal<string | null>(null);

  ngOnInit(): void {
    if (this.isOwner()) {
      this.form.get('ownerFullName')!.addValidators(Validators.required);
      this.form.get('ownerFullName')!.updateValueAndValidity();
    }
  }

  onNationalId(event: Event): void {
    this.nationalIdFile = (event.target as HTMLInputElement).files?.[0] ?? null;
  }

  onOwnershipProof(event: Event): void {
    this.ownershipProofFile = (event.target as HTMLInputElement).files?.[0] ?? null;
  }

  next(): void {
    this.createError.set(null);
    this.filesError.set(null);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    if (this.isOwner() && (!this.nationalIdFile || !this.ownershipProofFile)) {
      this.filesError.set('National ID and title deed images are required for owner submissions.');
      return;
    }

    this.creating.set(true);
    const v = this.form.getRawValue();

    this.propertySvc
      .create({
        ...v,
        listingSource: this.isOwner() ? 'Owner' : 'Company',
        ownerFullName: this.isOwner() ? v.ownerFullName : undefined,
        nationalIdFile: this.isOwner() ? this.nationalIdFile : null,
        ownershipProofFile: this.isOwner() ? this.ownershipProofFile : null,
      })
      .subscribe({
        next: ({ id }) => {
          this.creating.set(false);
          this.propertyId.set(id);
          this.loadAmenities();
          this.step.set(2);
        },
        error: (err) => {
          this.creating.set(false);
          this.createError.set(
            err?.error?.error ?? 'Failed to create the listing. Please try again.'
          );
        },
      });
  }

  private loadAmenities(): void {
    this.propertySvc.getAmenities().subscribe({
      next: (list) => this.amenities.set(list),
      error: () => this.amenities.set([]),
    });
  }

  onPhotosSelected(event: Event): void {
    const id = this.propertyId();
    if (!id) return;
    const files = Array.from((event.target as HTMLInputElement).files ?? []);

    files.forEach((file, i) => {
      const upload: PhotoUpload = {
        name: file.name,
        preview: URL.createObjectURL(file),
        status: 'uploading',
      };
      this.photos.update((p) => [...p, upload]);

      this.propertySvc.uploadMedia(id, file, this.photos().length - 1 + i).subscribe({
        next: () => this.setPhotoStatus(upload, 'done'),
        error: () => this.setPhotoStatus(upload, 'error'),
      });
    });
    (event.target as HTMLInputElement).value = '';
  }

  private setPhotoStatus(upload: PhotoUpload, status: PhotoUpload['status']): void {
    this.photos.update((list) =>
      list.map((p) => (p === upload ? { ...p, status } : p))
    );
  }

  toggleAmenity(amenity: Amenity): void {
    const id = this.propertyId();
    if (!id) return;
    const selected = new Set(this.selectedAmenities());

    if (selected.has(amenity.id)) {
      selected.delete(amenity.id);
      this.selectedAmenities.set(selected);
      this.propertySvc.removeAmenity(id, amenity.id).subscribe();
    } else {
      selected.add(amenity.id);
      this.selectedAmenities.set(selected);
      this.propertySvc.addAmenity(id, amenity.id).subscribe();
    }
  }

  generateDescription(): void {
    const id = this.propertyId();
    if (!id || this.generating()) return;

    this.generating.set(true);
    this.aiError.set(null);
    const previousId = this.draft()?.generationId ?? null;

    this.propertySvc.requestAiDescription(id).subscribe({
      next: () => this.pollDraft(id, previousId, 0),
      error: (err) => {
        this.generating.set(false);
        this.aiError.set(err?.error?.error ?? 'Could not start AI generation.');
      },
    });
  }

  private pollDraft(propertyId: string, previousId: string | null, attempt: number): void {
    if (attempt >= MAX_POLL_ATTEMPTS) {
      this.generating.set(false);
      this.aiError.set('The AI is taking longer than expected — try "Check again" in a moment.');
      return;
    }

    this.propertySvc.getLatestDescription(propertyId).subscribe({
      next: (draft) => {
        if (draft.generationId !== previousId) {
          this.draft.set(draft);
          this.textEn = draft.generatedTextEn ?? '';
          this.textAr = draft.generatedTextAr ?? '';
          this.accepted.set(draft.isAccepted);
          this.generating.set(false);
        } else {
          setTimeout(() => this.pollDraft(propertyId, previousId, attempt + 1), POLL_INTERVAL_MS);
        }
      },
      // 404 = not generated yet — keep polling
      error: () =>
        setTimeout(() => this.pollDraft(propertyId, previousId, attempt + 1), POLL_INTERVAL_MS),
    });
  }

  checkAgain(): void {
    const id = this.propertyId();
    if (!id) return;
    this.aiError.set(null);
    this.generating.set(true);
    this.pollDraft(id, this.draft()?.generationId ?? null, MAX_POLL_ATTEMPTS - 1);
  }

  acceptDescription(): void {
    const draft = this.draft();
    if (!draft || this.accepting()) return;

    this.accepting.set(true);
    this.aiError.set(null);

    this.propertySvc
      .acceptDescription(draft.generationId, this.textEn, this.textAr)
      .subscribe({
        next: () => {
          this.accepting.set(false);
          this.accepted.set(true);
        },
        error: (err) => {
          this.accepting.set(false);
          this.aiError.set(err?.error?.error ?? 'Failed to save the description.');
        },
      });
  }

  submit(): void {
    const id = this.propertyId();
    if (!id || this.submitting()) return;

    this.submitting.set(true);
    this.submitError.set(null);

    this.propertySvc.submitForReview(id).subscribe({
      next: () => {
        this.submitting.set(false);
        this.step.set(3);
      },
      error: (err) => {
        this.submitting.set(false);
        this.submitError.set(err?.error?.error ?? 'Failed to submit the listing for review.');
      },
    });
  }
}
