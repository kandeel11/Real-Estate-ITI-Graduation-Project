import { Component, Input, Output, EventEmitter, OnDestroy, inject, signal } from '@angular/core';
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
export class LeaseUploadModalComponent implements OnDestroy {
  @Input({ required: true }) lead!: Lead;
  @Output() closed = new EventEmitter<void>();
  @Output() leaseCreated = new EventEmitter<void>();

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

  leaseId = signal<string | null>(null);

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
    if (!email) { this.uploadError.set('BROKER.LEASE_UPLOAD.ERR_NO_EMAIL'); return; }

    this.uploading.set(true);
    this.uploadError.set(null);
    const propId = this.lead.propertyId ?? '';

    this.svc.uploadLeasePdf(propId, email, this.file).subscribe({
      next: ({ leaseId }) => {
        this.leaseId.set(leaseId);
        const doCheck = () => {
          this.svc.getLeaseAiExtraction(leaseId).subscribe({
            next: ext => {
              if (ext.extractionStatus !== 'pending') {
                this.stopPolling();
                this.extractionFailed.set(ext.extractionStatus === 'failed');
                this.extraction.set(ext);
                this.uploading.set(false);
                this.step.set(2);
              }
            },
            error: () => {
              this.stopPolling();
              this.uploading.set(false);
              this.uploadError.set('BROKER.LEASE_UPLOAD.UPLOAD_ERROR');
            },
          });
        };
        // Assign pollId BEFORE calling doCheck() so stopPolling() always sees a non-null id,
        // even when the mock observable resolves synchronously inside doCheck().
        this.pollId = setInterval(doCheck, 3000);
        doCheck();
      },
      error: () => { this.uploading.set(false); this.uploadError.set('BROKER.LEASE_UPLOAD.UPLOAD_ERROR'); },
    });
  }

  sendToTenant(): void {
    const ext = this.extraction();
    const id = this.leaseId();
    if (!ext || !id || this.sending()) return;
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
