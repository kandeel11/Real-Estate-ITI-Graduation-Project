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

import {
  Component,
  inject,
  signal,
  computed,
  OnInit,
  HostListener,
  ElementRef,
} from '@angular/core';
import {
  ReactiveFormsModule,
  FormsModule,
  FormBuilder,
  Validators,
  AbstractControl,
  ValidationErrors,
} from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { AuthService } from '../../../../core/services/auth.service';
import { COUNTRIES, STATES_BY_COUNTRY } from './geo-data';
import { PropertyService } from '../../services/property.service';
import { CompanySubscriptionService } from '../../../dashboards/company/services/company-subscription.service';
import { Amenity, DescriptionDraft, Property } from '../../models/property.model';
import { TranslatePipe } from '@ngx-translate/core';

interface PhotoUpload {
  name: string;
  preview: string;
  status: 'uploading' | 'done' | 'error' | 'removing';
  mediaId?: string;
  isPrimary: boolean;
}

const MAX_POLL_ATTEMPTS = 20;
const POLL_INTERVAL_MS = 3000;

const ARABIC = /[؀-ۿ]/;
const LATIN = /[a-zA-Z]/;

/** English-only field: rejects any Arabic character (digits/punctuation are fine). */
function englishOnly(control: AbstractControl): ValidationErrors | null {
  return ARABIC.test(control.value ?? '') ? { mustBeEnglish: true } : null;
}

/** Arabic-only field: rejects any Latin letter. */
function arabicOnly(control: AbstractControl): ValidationErrors | null {
  return LATIN.test(control.value ?? '') ? { mustBeArabic: true } : null;
}

@Component({
  selector: 'app-create-property',
  standalone: true,
  imports: [ReactiveFormsModule, FormsModule, RouterLink, TranslatePipe],
  templateUrl: './create-property.component.html',
  styleUrl: './create-property.component.css',
})
export class CreatePropertyComponent implements OnInit {
  private fb = inject(FormBuilder);
  private propertySvc = inject(PropertyService);
  private auth = inject(AuthService);
  private subSvc = inject(CompanySubscriptionService);
  private route = inject(ActivatedRoute);
  private elRef = inject(ElementRef);

  // ── Country / Governorate autocomplete ─────────────────────────────
  allCountries = signal<string[]>([]);
  filteredCountries = signal<string[]>([]);
  allGovernorates = signal<string[]>([]);
  filteredGovernorates = signal<string[]>([]);
  countryDropdownOpen = signal(false);
  governorateDropdownOpen = signal(false);

  @HostListener('document:click', ['$event'])
  onDocClick(e: MouseEvent): void {
    const el = this.elRef.nativeElement as HTMLElement;
    if (!el.contains(e.target as Node)) {
      this.countryDropdownOpen.set(false);
      this.governorateDropdownOpen.set(false);
    }
  }

  private loadCountries(): void {
    this.allCountries.set(COUNTRIES);
    this.filteredCountries.set(COUNTRIES);
  }

  onCountryInput(event: Event): void {
    const val = (event.target as HTMLInputElement).value;
    this.form.get('country')!.setValue(val);
    this.filteredCountries.set(
      this.allCountries().filter((c) => c.toLowerCase().includes(val.toLowerCase())),
    );
    this.countryDropdownOpen.set(true);
    this.form.get('governorate')!.setValue('');
    this.allGovernorates.set([]);
    this.filteredGovernorates.set([]);
  }

  selectCountry(country: string): void {
    this.form.get('country')!.setValue(country);
    this.countryDropdownOpen.set(false);
    this.form.get('governorate')!.setValue('');
    this.allGovernorates.set([]);
    this.filteredGovernorates.set([]);
    this.loadGovernorates(country);
  }

  loadGovernorates(country: string): void {
    if (!country) return;
    const states = STATES_BY_COUNTRY[country] ?? [];
    this.allGovernorates.set(states);
    this.filteredGovernorates.set(states);
  }

  onGovernorateInput(event: Event): void {
    const val = (event.target as HTMLInputElement).value;
    this.form.get('governorate')!.setValue(val);
    this.filteredGovernorates.set(
      this.allGovernorates().filter((g) => g.toLowerCase().includes(val.toLowerCase())),
    );
    this.governorateDropdownOpen.set(true);
  }

  selectGovernorate(gov: string): void {
    this.form.get('governorate')!.setValue(gov);
    this.governorateDropdownOpen.set(false);
  }

  step = signal<1 | 2 | 3>(1);
  isOwner = computed(() => this.auth.userRole() === 'owner');

  /** Plan gate for the AI generator — free tiers write descriptions manually. */
  private subscription = toSignal(this.subSvc.getActive());
  aiGeneratorAllowed = computed(() => !!this.subscription()?.hasAiDescriptionGenerator);

  // ── Step 1: details ─────────────────────────────────────────────
  creating = signal(false);
  createError = signal<string | null>(null);
  propertyId = signal<string | null>(null);
  nationalIdFile: File | null = null;
  ownershipProofFile: File | null = null;
  nationalIdFileName = signal<string | null>(null);
  ownershipProofFileName = signal<string | null>(null);
  filesError = signal<string | null>(null);

  form = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.minLength(5), englishOnly]],
    titleAr: ['', [Validators.required, arabicOnly]],
    propertyType: ['Apartment', Validators.required],
    price: [0, [Validators.required, Validators.min(1)]],
    bedrooms: [1, [Validators.required, Validators.min(0)]],
    bathrooms: [1, [Validators.required, Validators.min(0)]],
    areaSqm: [50, [Validators.required, Validators.min(1)]],
    floorNumber: [0, Validators.min(0)],
    totalFloors: [1, Validators.min(1)],
    yearBuilt: [2020, [Validators.min(1900), Validators.max(2026)]],
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

  /** Amenities grouped by their seed category (Pool, Security, Outdoor…). */
  amenityGroups = computed(() => {
    const groups = new Map<string, Amenity[]>();
    for (const amenity of this.amenities()) {
      const list = groups.get(amenity.category) ?? [];
      list.push(amenity);
      groups.set(amenity.category, list);
    }
    return [...groups.entries()].map(([category, items]) => ({ category, items }));
  });

  generating = signal(false);
  aiError = signal<string | null>(null);
  draft = signal<DescriptionDraft | null>(null);
  textEn = signal('');
  textAr = signal('');
  accepted = signal(false);
  accepting = signal(false);

  // Live script validation for the description boxes
  descEnInvalid = computed(() => ARABIC.test(this.textEn()));
  descArInvalid = computed(() => LATIN.test(this.textAr()));

  submitting = signal(false);
  submitError = signal<string | null>(null);
  finalStatus = signal<string>('PendingReview');

  /** Edit mode: the property already exists (after create, or on resume). */
  isEditing = computed(() => this.propertyId() !== null);

  // ── Resume / draft recovery ─────────────────────────────────────
  resuming = signal(false);
  pendingDraft = signal<Property | null>(null);
  discardingDraft = signal(false);

  ngOnInit(): void {
    this.loadCountries();
    // Pre-load governorates for the default country (Egypt)
    this.loadGovernorates(this.form.get('country')!.value);

    if (this.isOwner()) {
      this.form.get('ownerFullName')!.addValidators(Validators.required);
      this.form.get('ownerFullName')!.updateValueAndValidity();
    }

    const resumeId = this.route.snapshot.queryParamMap.get('propertyId');
    if (resumeId) {
      this.resume(resumeId);
    } else {
      // Fresh open: surface the most recent abandoned draft, if any
      this.propertySvc.getMine().subscribe({
        next: (list) => {
          const draft = list
            .filter((p) => p.status === 'Draft')
            .sort((a, b) => b.createdAt.localeCompare(a.createdAt))[0];
          if (draft) this.pendingDraft.set(draft);
        },
        error: () => {}, // banner is best-effort only
      });
    }
  }

  /** Reopens an existing Draft at step 2 with everything prefilled. */
  resume(id: string): void {
    this.resuming.set(true);
    this.propertySvc.getById(id).subscribe({
      next: (detail) => {
        this.form.patchValue({
          title: detail.title,
          titleAr: detail.titleAr,
          propertyType: detail.propertyType,
          price: detail.price,
          bedrooms: detail.bedrooms,
          bathrooms: detail.bathrooms,
          areaSqm: detail.areaSqm,
          floorNumber: detail.floorNumber,
          totalFloors: detail.totalFloors,
          yearBuilt: detail.yearBuilt,
          isFurnished: detail.isFurnished,
          petsAllowed: detail.petsAllowed,
          address: detail.address,
          apartmentNumber: detail.apartmentNumber,
          country: detail.country,
          governorate: detail.governorate,
          city: detail.city,
          district: detail.district,
        });

        this.enterEditMode(detail.id);
        this.textEn.set(detail.description ?? '');
        this.textAr.set(detail.descriptionAr ?? '');
        this.accepted.set(!!(detail.description || detail.descriptionAr));
        this.selectedAmenities.set(new Set(detail.amenities.map((a) => a.id)));
        this.photos.set(
          detail.media.map((m) => ({
            name: m.mediaUrl,
            preview: m.mediaUrl,
            status: 'done' as const,
            mediaId: m.id,
            isPrimary: m.isPrimary,
          })),
        );

        this.loadAmenities();
        this.pendingDraft.set(null);
        this.resuming.set(false);
        this.step.set(2);
      },
      error: () => {
        this.resuming.set(false);
        this.createError.set('Could not load the draft listing — it may have been deleted.');
      },
    });
  }

  discardDraft(): void {
    const draft = this.pendingDraft();
    if (!draft || this.discardingDraft()) return;
    this.discardingDraft.set(true);
    this.propertySvc.delete(draft.id).subscribe({
      next: () => {
        this.discardingDraft.set(false);
        this.pendingDraft.set(null);
      },
      error: () => {
        this.discardingDraft.set(false);
        this.pendingDraft.set(null);
      },
    });
  }

  onNationalId(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0] ?? null;
    this.nationalIdFile = file;
    this.nationalIdFileName.set(file?.name ?? null);
    this.filesError.set(null);
  }

  onOwnershipProof(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0] ?? null;
    this.ownershipProofFile = file;
    this.ownershipProofFileName.set(file?.name ?? null);
    this.filesError.set(null);
  }

  clearNationalId(inputEl: HTMLInputElement): void {
    this.nationalIdFile = null;
    this.nationalIdFileName.set(null);
    inputEl.value = '';
  }

  clearOwnershipProof(inputEl: HTMLInputElement): void {
    this.ownershipProofFile = null;
    this.ownershipProofFileName.set(null);
    inputEl.value = '';
  }

  /**
   * Step 1 → step 2. Creates the listing the first time; on subsequent visits
   * (Back, or a resumed draft) it updates the existing listing instead, so
   * editing details never creates a duplicate.
   */
  next(): void {
    this.createError.set(null);
    this.filesError.set(null);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    if (this.isEditing()) {
      this.saveDetailsAndContinue();
      return;
    }

    // ── First-time create (owners must attach the ownership documents) ──
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
          this.enterEditMode(id);
          this.loadAmenities();
          this.step.set(2);
        },
        error: (err) => {
          this.creating.set(false);
          this.createError.set(
            err?.error?.error ?? 'Failed to create the listing. Please try again.',
          );
        },
      });
  }

  /** Persists step-1 edits to the existing listing, then returns to step 2. */
  private saveDetailsAndContinue(): void {
    const id = this.propertyId();
    if (!id) return;

    this.creating.set(true);
    this.createError.set(null);

    const v = this.form.getRawValue();
    const listingBrokerId =
      this.auth.userRole() === 'broker' ? (this.auth.user()?.id ?? null) : null;

    this.propertySvc
      .update(
        id,
        { ...v, listingSource: this.isOwner() ? 'Owner' : 'Company' },
        this.textEn(),
        this.textAr(),
        listingBrokerId,
      )
      .subscribe({
        next: () => {
          this.creating.set(false);
          this.step.set(2);
        },
        error: (err) => {
          this.creating.set(false);
          this.createError.set(err?.error?.error ?? 'Failed to save your changes.');
        },
      });
  }

  /** Step 2 → step 1, to edit the property details. */
  goBack(): void {
    this.step.set(1);
  }

  /**
   * Switches the wizard into edit mode for an existing listing: remembers the
   * id and drops the owner-document requirement (the deed was already
   * verified at creation; updates don't re-upload files).
   */
  private enterEditMode(id: string): void {
    this.propertyId.set(id);
    const ownerName = this.form.get('ownerFullName')!;
    ownerName.clearValidators();
    ownerName.updateValueAndValidity();
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
    const currentCount = this.photos().length;

    files.forEach((file, i) => {
      const isFirst = currentCount === 0 && i === 0;
      const upload: PhotoUpload = {
        name: file.name,
        preview: URL.createObjectURL(file),
        status: 'uploading',
        isPrimary: isFirst,
      };
      this.photos.update((p) => [...p, upload]);

      this.propertySvc.uploadMedia(id, file, currentCount + i).subscribe({
        next: (res) => {
          this.photos.update((list) =>
            list.map((p) => (p === upload ? { ...p, status: 'done', mediaId: res.mediaId } : p)),
          );
          // Auto-set the first ever uploaded photo as primary in the DB
          if (isFirst) {
            this.propertySvc.setPrimaryMedia(id, res.mediaId).subscribe();
          }
        },
        error: () => this.setPhotoStatus(upload, 'error'),
      });
    });
    (event.target as HTMLInputElement).value = '';
  }

  /**
   * Removes a photo the user no longer wants. If it finished uploading it's deleted
   * from the backend (and Azure Blob Storage) first; an in-flight/errored upload with
   * no media id is just dropped locally. Removing the primary promotes the next photo.
   */
  removePhoto(target: PhotoUpload): void {
    if (target.status === 'removing') return;
    const id = this.propertyId();

    // No persisted media yet (still uploading, or it errored) → drop it locally.
    if (!id || !target.mediaId) {
      this.photos.update((list) => list.filter((p) => p !== target));
      return;
    }

    const wasPrimary = target.isPrimary;
    this.setPhotoStatus(target, 'removing');
    this.propertySvc.deleteMedia(id, target.mediaId).subscribe({
      next: () => {
        this.photos.update((list) => list.filter((p) => p !== target));
        // If we removed the primary, promote the first remaining uploaded photo.
        if (wasPrimary) {
          const next = this.photos().find((p) => p.mediaId && p.status === 'done');
          if (next) this.setPrimaryPhoto(next);
        }
      },
      // Couldn't delete (e.g. network/Azure) — restore the tile so nothing is silently lost.
      error: () => this.setPhotoStatus(target, 'done'),
    });
  }

  setPrimaryPhoto(target: PhotoUpload): void {
    const id = this.propertyId();
    if (!id || !target.mediaId || target.isPrimary) return;
    this.propertySvc.setPrimaryMedia(id, target.mediaId).subscribe({
      next: () => {
        this.photos.update((list) => list.map((p) => ({ ...p, isPrimary: p === target })));
      },
    });
  }

  private setPhotoStatus(upload: PhotoUpload, status: PhotoUpload['status']): void {
    this.photos.update((list) => list.map((p) => (p === upload ? { ...p, status } : p)));
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
          this.textEn.set(draft.generatedTextEn ?? '');
          this.textAr.set(draft.generatedTextAr ?? '');
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

  /** True when the typed descriptions don't match their required script. */
  private descriptionScriptError(): string | null {
    if (this.descEnInvalid()) return 'The English description must not contain Arabic text.';
    if (this.descArInvalid()) return 'The Arabic description must not contain English letters.';
    return null;
  }

  acceptDescription(): void {
    const draft = this.draft();
    if (!draft || this.accepting()) return;

    const scriptError = this.descriptionScriptError();
    if (scriptError) {
      this.aiError.set(scriptError);
      return;
    }

    this.accepting.set(true);
    this.aiError.set(null);

    this.propertySvc.acceptDescription(draft.generationId, this.textEn(), this.textAr()).subscribe({
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

  /**
   * Manual path (free tiers without the AI generator): saves the typed
   * descriptions via the regular property update, passing the step-1 values
   * back through since PUT requires the full body.
   */
  saveManualDescription(): void {
    const id = this.propertyId();
    if (!id || this.accepting()) return;
    if (!this.textEn().trim() && !this.textAr().trim()) {
      this.aiError.set('Write a description first.');
      return;
    }

    const scriptError = this.descriptionScriptError();
    if (scriptError) {
      this.aiError.set(scriptError);
      return;
    }

    this.accepting.set(true);
    this.aiError.set(null);

    const v = this.form.getRawValue();
    // Brokers keep their own listing assignment; owners have none
    const listingBrokerId =
      this.auth.userRole() === 'broker' ? (this.auth.user()?.id ?? null) : null;

    this.propertySvc
      .update(
        id,
        { ...v, listingSource: this.isOwner() ? 'Owner' : 'Company' },
        this.textEn(),
        this.textAr(),
        listingBrokerId,
      )
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
        this.finalStatus.set('PendingReview');
        this.step.set(3);
      },
      error: (err) => {
        const msg: string = err?.error?.error ?? '';
        // The ownership AI runs asynchronously right after property creation.
        // If it already moved the property past Draft (e.g. Active via auto-verify,
        // or Rejected which stays Draft with our backend fix), submitForReview may
        // return a 400/422. Fetch the actual status and proceed to the confirmation
        // step if it is no longer a plain Draft — the listing was effectively submitted.
        this.propertySvc.getById(id).subscribe({
          next: (detail) => {
            this.submitting.set(false);
            if (detail.status !== 'Draft') {
              this.finalStatus.set(detail.status);
              this.step.set(3);
            } else {
              this.submitError.set(msg || 'Failed to submit the listing for review.');
            }
          },
          error: () => {
            this.submitting.set(false);
            this.submitError.set(msg || 'Failed to submit the listing for review.');
          },
        });
      },
    });
  }
}
