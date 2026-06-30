import { Component, Input, Output, EventEmitter, OnInit, OnDestroy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { OwnerDataService } from '../../services/owner-data.service';
import { Lead, LeaseAiExtraction } from '../../models/owner.models';
import { AuthService } from '../../../../../core/services/auth.service';

type Step = 1 | 2 | 3;

@Component({
  selector: 'app-owner-lease-upload-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe],
  templateUrl: './lease-upload-modal.component.html',
  styleUrl: './lease-upload-modal.component.css',
})
export class LeaseUploadModalComponent implements OnInit, OnDestroy {
  @Input({ required: true }) lead!: Lead;
  /** When set, runs in renewal mode: uploads against this lease via /leases/{id}/renew-upload. */
  @Input() renewLeaseId: string | null = null;
  @Output() closed = new EventEmitter<void>();
  @Output() leaseCreated = new EventEmitter<void>();

  get isRenewal(): boolean { return !!this.renewLeaseId; }

  /**
   * When set, the modal *resumes* an existing un-sent lease: it skips the upload step,
   * loads that lease's AI extraction, and opens straight on the review step.
   */
  @Input() resumeLeaseId: string | null = null;
  get isResume(): boolean { return !!this.resumeLeaseId; }

  private readonly svc = inject(OwnerDataService);
  private readonly auth = inject(AuthService);
  private pollId: ReturnType<typeof setInterval> | null = null;

  step = signal<Step>(1);
  file: File | null = null;
  fileName = signal('');
  tenantEmail = signal('');
  uploading = signal(false);
  uploadError = signal<string | null>(null);

  extraction = signal<LeaseAiExtraction | null>(null);
  extractionFailed = signal(false);
  /** Diagnosis message from /extraction-debug when key fields are null after completion. */
  extractionDiagnosis = signal<string | null>(null);
  sending = signal(false);
  /** Loading an existing lease's extraction while resuming (before the review step shows). */
  resuming = signal(false);

  leaseId = signal<string | null>(null);

  /**
   * Validates the editable lease dates before sending. Returns an i18n key for
   * the problem, or null when valid. The lease can't be sent without both dates
   * (and the end must be after the start).
   */
  readonly dateError = computed<string | null>(() => {
    const e = this.extraction();
    if (!e) return null;
    if (!e.startDate || !e.endDate) return 'OWNER.LEASE_UPLOAD.ERR_DATES_REQUIRED';
    if (new Date(e.endDate) <= new Date(e.startDate)) return 'OWNER.LEASE_UPLOAD.ERR_DATES_ORDER';
    return null;
  });

  ngOnInit(): void {
    // The tenant's email comes from their account (resolved via the lead's TenantId),
    // so the owner never types it.
    this.tenantEmail.set(this.lead.tenantEmail ?? '');

    // Resume mode: jump straight to the review step for an already-uploaded lease.
    if (this.resumeLeaseId) this.resumeExisting(this.resumeLeaseId);
  }

  /** Resume an existing un-sent lease: load its extraction, then show the review step. */
  private resumeExisting(leaseId: string): void {
    this.resuming.set(true);
    this.leaseId.set(leaseId);
    this.beginExtractionPolling(leaseId, () => this.resuming.set(false));
  }

  /**
   * Polls /extraction-debug to detect completion, then always loads /ai-extraction
   * for the real data. isReadyForFrontend drives a warning only — never blocks the form.
   * Falls back to polling /ai-extraction directly if the debug endpoint is unavailable.
   */
  private beginExtractionPolling(leaseId: string, onSettled?: () => void): void {
    const openReview = (ext: LeaseAiExtraction, failed: boolean, diagnosis?: string) => {
      const userName = this.auth.user()?.fullName ?? '';
      this.extraction.set({
        ...ext,
        tenantEmail: this.lead.tenantEmail ?? '',
        ownerName: ext.ownerName || userName,
      });
      this.extractionFailed.set(failed);
      this.extractionDiagnosis.set(diagnosis ?? null);
      this.uploading.set(false);
      onSettled?.();
      this.step.set(2);
    };

    const doCheck = () => {
      this.svc.getLeaseExtractionDebug(leaseId).subscribe({
        next: (debug) => {
          if (debug.displayStatus === 'not_rent_lease') {
            this.stopPolling();
            this.uploading.set(false);
            onSettled?.();
            this.uploadError.set('OWNER.LEASE_UPLOAD.ERR_NOT_RENT_LEASE');
            return;
          }
          if (debug.displayStatus === 'pending') return;

          // Stop the interval immediately — don't let it fire again while the HTTP is in-flight.
          this.stopPolling();

          // Always load the real data; use isReadyForFrontend only for the warning badge.
          this.svc.getLeaseAiExtraction(leaseId).subscribe({
            next: (ext) => {
              const warn = !debug.isReadyForFrontend ? debug.diagnosis : undefined;
              openReview(ext, debug.displayStatus === 'failed', warn);
            },
            error: () => openReview(
              { tenantName: '', tenantEmail: '', ownerName: '', propertyTitle: '',
                startDate: '', endDate: '', installmentAmount: 0,
                paymentFrequency: 'Monthly', deposit: 0,
                extractionStatus: 'failed', confidence: 0 },
              true, 'Extraction completed but data could not be loaded.',
            ),
          });
        },
        error: () => {
          // Debug endpoint unavailable — fall back to polling /ai-extraction directly.
          this.svc.getLeaseAiExtraction(leaseId).subscribe({
            next: (ext) => {
              if (ext.extractionStatus === 'pending') return;
              this.stopPolling();
              openReview(ext, ext.extractionStatus === 'failed');
            },
            error: () => {
              this.stopPolling();
              this.uploading.set(false);
              onSettled?.();
              this.uploadError.set('OWNER.LEASE_UPLOAD.UPLOAD_ERROR');
            },
          });
        },
      });
    };
    this.pollId = setInterval(doCheck, 3000);
    doCheck();
  }

  ngOnDestroy(): void {
    this.stopPolling();
  }

  private stopPolling(): void {
    if (this.pollId !== null) {
      clearInterval(this.pollId);
      this.pollId = null;
    }
  }

  onFileChange(e: Event): void {
    const f = (e.target as HTMLInputElement).files?.[0] ?? null;
    this.file = f;
    this.fileName.set(f?.name ?? '');
    this.uploadError.set(null);
  }

  upload(): void {
    if (!this.file) { this.uploadError.set('OWNER.LEASE_UPLOAD.ERR_NO_FILE'); return; }
    const email = this.tenantEmail().trim();
    if (!email && !this.isRenewal) { this.uploadError.set('OWNER.LEASE_UPLOAD.ERR_NO_EMAIL'); return; }

    this.uploading.set(true);
    this.uploadError.set(null);

    const upload$ = this.isRenewal
      ? this.svc.renewLeaseUpload(this.renewLeaseId!, this.file)
      : this.svc.uploadLeasePdf(this.lead.id, email, this.file);

    upload$.subscribe({
      next: ({ leaseId }) => {
        this.leaseId.set(leaseId);
        this.beginExtractionPolling(leaseId);
      },
      error: () => { this.uploading.set(false); this.uploadError.set('OWNER.LEASE_UPLOAD.UPLOAD_ERROR'); },
    });
  }

  sendToTenant(): void {
    const ext = this.extraction();
    const id = this.leaseId();
    if (!ext || !id || this.sending() || this.dateError()) return;
    this.sending.set(true);
    this.svc.sendLeaseToTenant(id, ext).subscribe({
      next: () => { this.sending.set(false); this.step.set(3); },
      error: () => this.sending.set(false),
    });
  }

  done(): void {
    this.leaseCreated.emit();
  }

  updateField(field: keyof LeaseAiExtraction, value: string | number): void {
    const ext = this.extraction();
    if (ext) this.extraction.set({ ...ext, [field]: value });
  }

  fieldConf(field: keyof NonNullable<LeaseAiExtraction['fieldConfidence']>): number | undefined {
    return this.extraction()?.fieldConfidence?.[field];
  }

  confBadgeClass(score: number | undefined): string {
    if (score === undefined) return 'conf-badge conf-na';
    if (score >= 85) return 'conf-badge conf-high';
    if (score >= 70) return 'conf-badge conf-med';
    return 'conf-badge conf-low';
  }
}
