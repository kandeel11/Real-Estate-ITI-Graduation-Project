import { Component, Input, Output, EventEmitter, OnInit, OnDestroy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { BrokerDataService } from '../../services/broker-data.service';
import { Lead, LeaseAiExtraction } from '../../models/broker.models';

type Step = 1 | 2 | 3;

@Component({
  selector: 'app-lease-upload-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe],
  templateUrl: './lease-upload-modal.component.html',
  styleUrl: './lease-upload-modal.component.css',
})
export class LeaseUploadModalComponent implements OnInit, OnDestroy {
  @Input({ required: true }) lead!: Lead;
  /**
   * When set, the modal runs in *renewal* mode: it uploads against this existing
   * lease (POST /leases/{id}/renew-upload) instead of creating one from the lead.
   * The AI-extraction → send-to-tenant flow afterwards is identical.
   */
  @Input() renewLeaseId: string | null = null;
  @Output() closed = new EventEmitter<void>();
  @Output() leaseCreated = new EventEmitter<void>();

  get isRenewal(): boolean { return !!this.renewLeaseId; }

  /**
   * When set, the modal *resumes* an existing un-sent lease: it skips the upload step,
   * loads that lease's AI extraction, and opens straight on the review step so the
   * broker can finish a lease they started earlier.
   */
  @Input() resumeLeaseId: string | null = null;
  get isResume(): boolean { return !!this.resumeLeaseId; }

  private readonly svc = inject(BrokerDataService);
  private pollId: ReturnType<typeof setInterval> | null = null;

  step = signal<Step>(1);
  file: File | null = null;
  fileName = signal('');
  tenantEmail = signal('');
  uploading = signal(false);
  uploadError = signal<string | null>(null);

  extraction = signal<LeaseAiExtraction | null>(null);
  extractionFailed = signal(false);
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
    if (!e.startDate || !e.endDate) return 'BROKER.LEASE_UPLOAD.ERR_DATES_REQUIRED';
    if (new Date(e.endDate) <= new Date(e.startDate)) return 'BROKER.LEASE_UPLOAD.ERR_DATES_ORDER';
    return null;
  });

  ngOnInit(): void {
    // The tenant's email comes from their account (resolved via the lead's TenantId),
    // so the broker never types it.
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
   * Polls the lease's AI extraction until it settles, then opens the review step.
   * Shared by a fresh upload and by resuming an existing lease.
   */
  private beginExtractionPolling(leaseId: string, onSettled?: () => void): void {
    const doCheck = () => {
      this.svc.getLeaseAiExtraction(leaseId).subscribe({
        next: (ext) => {
          if (ext.extractionStatus !== 'pending') {
            this.stopPolling();
            this.extractionFailed.set(ext.extractionStatus === 'failed');
            this.extraction.set(ext);
            this.uploading.set(false);
            onSettled?.();
            this.step.set(2);
          }
        },
        error: () => {
          this.stopPolling();
          this.uploading.set(false);
          onSettled?.();
          this.uploadError.set('BROKER.LEASE_UPLOAD.UPLOAD_ERROR');
        },
      });
    };
    // Assign pollId BEFORE the first check so stopPolling() always sees a non-null id,
    // even when the mock observable resolves synchronously inside doCheck().
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
    if (!this.file) { this.uploadError.set('BROKER.LEASE_UPLOAD.ERR_NO_FILE'); return; }
    const email = this.tenantEmail().trim();
    if (!email && !this.isRenewal) { this.uploadError.set('BROKER.LEASE_UPLOAD.ERR_NO_EMAIL'); return; }

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
      error: () => { this.uploading.set(false); this.uploadError.set('BROKER.LEASE_UPLOAD.UPLOAD_ERROR'); },
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
}
