import {
  Component,
  OnInit,
  OnDestroy,
  inject,
  signal,
  computed,
} from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { SlicePipe } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { MaintenanceService, TicketPriority } from '../../../services/maintenance.service';
import { LeaseService, ApiLease } from '../../../services/lease.service';
import { ProfileService } from '../../../services/profile.service';
import { ToastService } from '../../../../../../shared/components/toast/toast.service';
import { TranslatePipe } from '@ngx-translate/core';

export interface UploadedFileItem {
  file: File;
  previewUrl: string;
  status: 'uploading' | 'done' | 'error';
  attachmentId?: string;
  error?: string;
}

@Component({
  selector: 'app-create-ticket',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, SlicePipe, TranslatePipe],
  templateUrl: './create-ticket.component.html',
  styleUrl: './create-ticket.component.css',
})
export class CreateTicketComponent implements OnInit, OnDestroy {
  private readonly maintenanceService = inject(MaintenanceService);
  private readonly leaseService = inject(LeaseService);
  private readonly profileService = inject(ProfileService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  // ── Step state ──────────────────────────────────────────────────────────
  protected step = signal<1 | 2 | 3>(1);
  protected ticketId = signal<string | null>(null);
  protected activeLease = signal<ApiLease | null>(null);
  protected leaseLoading = signal(true);
  protected step1Loading = signal(false);

  // ── Step 2 ──────────────────────────────────────────────────────────────
  protected uploadedFiles = signal<UploadedFileItem[]>([]);
  protected dragOver = signal(false);

  // ── Step 3 ──────────────────────────────────────────────────────────────
  protected submitting = signal(false);
  protected pollingMessage = signal<string | null>(null);
  protected submitSuccess = signal(false);
  protected submittedTicketId = signal<string | null>(null);
  private pollInterval: ReturnType<typeof setInterval> | null = null;
  private pollRetries = 0;

  // ── Forms ────────────────────────────────────────────────────────────────
  readonly basicForm = this.fb.group({
    title:       ['', [Validators.required, Validators.minLength(5)]],
    description: ['', [Validators.required, Validators.minLength(20)]],
    priority:    ['Medium' as TicketPriority, Validators.required],
  });

  readonly contactForm = this.fb.group({
    propertyAddress: ['', Validators.required],
    tenantPhone:     ['', Validators.required],
    tenantEmail:     ['', [Validators.required, Validators.email]],
  });

  // ── Computed helpers ──────────────────────────────────────────────────────
  protected readonly descLength = computed(() =>
    this.basicForm.get('description')?.value?.length ?? 0
  );

  protected readonly hasSuccessfulUpload = computed(() =>
    this.uploadedFiles().some(f => f.status === 'done')
  );

  protected readonly allUploadsSettled = computed(() =>
    this.uploadedFiles().every(f => f.status !== 'uploading')
  );

  ngOnInit(): void {
    this.leaseService.getActiveLease().subscribe({
      next: lease => {
        this.activeLease.set(lease);
        this.leaseLoading.set(false);
      },
      error: () => this.leaseLoading.set(false),
    });

    this.profileService.getProfile().subscribe({
      next: profile => {
        this.contactForm.patchValue({
          tenantEmail: profile.email,
          tenantPhone: profile.phone,
        });
      },
    });
  }

  ngOnDestroy(): void {
    this.clearPoll();
  }

  // ── Step 1 → Create ticket → Step 2 ──────────────────────────────────────
  nextStep1(): void {
    if (this.basicForm.invalid) {
      this.basicForm.markAllAsTouched();
      return;
    }
    const lease = this.activeLease();
    if (!lease) {
      this.toast.error('No active lease found. Cannot create a maintenance ticket.');
      return;
    }

    this.step1Loading.set(true);
    const val = this.basicForm.getRawValue();
    this.maintenanceService.createTicket({
      propertyId:  lease.propertyId,
      ownerId:     lease.ownerId,
      leaseId:     lease.id,
      title:       val.title!,
      description: val.description!,
      priority:    val.priority as TicketPriority,
    }).pipe(finalize(() => this.step1Loading.set(false))).subscribe({
      next: res => {
        this.ticketId.set(res.id);
        this.step.set(2);
      },
      error: () => this.toast.error('Failed to create ticket. Please try again.'),
    });
  }

  // ── Step 2 file handling ──────────────────────────────────────────────────
  onFileSelect(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files) this.addFiles(Array.from(input.files));
    input.value = '';
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.dragOver.set(false);
    if (event.dataTransfer?.files) this.addFiles(Array.from(event.dataTransfer.files));
  }

  private addFiles(files: File[]): void {
    const id = this.ticketId();
    if (!id) return;

    const images = files.filter(f =>
      ['image/jpeg', 'image/png', 'image/webp'].includes(f.type) && f.size <= 10 * 1024 * 1024
    );

    images.forEach(file => {
      const reader = new FileReader();
      reader.onload = e => {
        const item: UploadedFileItem = {
          file,
          previewUrl: e.target!.result as string,
          status: 'uploading',
        };
        this.uploadedFiles.update(cur => [...cur, item]);
        const idx = this.uploadedFiles().length - 1;
        this.uploadFile(id, idx);
      };
      reader.readAsDataURL(file);
    });

    const oversized = files.filter(f => f.size > 10 * 1024 * 1024);
    if (oversized.length) this.toast.warning(`${oversized.length} file(s) skipped — max 10 MB each.`);
  }

  private uploadFile(ticketId: string, index: number): void {
    const item = this.uploadedFiles()[index];
    if (!item) return;

    this.maintenanceService.uploadAttachment(ticketId, item.file).subscribe({
      next: res => {
        this.uploadedFiles.update(cur =>
          cur.map((f, i) => i === index ? { ...f, status: 'done', attachmentId: res.attachmentId } : f)
        );
      },
      error: () => {
        this.uploadedFiles.update(cur =>
          cur.map((f, i) => i === index ? { ...f, status: 'error', error: 'Upload failed' } : f)
        );
      },
    });
  }

  removeFile(index: number): void {
    this.uploadedFiles.update(cur => cur.filter((_, i) => i !== index));
  }

  retryFile(index: number): void {
    const id = this.ticketId();
    if (!id) return;
    this.uploadedFiles.update(cur =>
      cur.map((f, i) => i === index ? { ...f, status: 'uploading', error: undefined } : f)
    );
    this.uploadFile(id, index);
  }

  nextStep2(): void {
    if (!this.hasSuccessfulUpload()) {
      this.toast.warning('Please upload at least one image before continuing.');
      return;
    }
    this.step.set(3);
  }

  // ── Step 3 → Submit ───────────────────────────────────────────────────────
  submit(): void {
    if (this.contactForm.invalid) {
      this.contactForm.markAllAsTouched();
      return;
    }
    const id = this.ticketId();
    if (!id) return;

    this.submitting.set(true);
    const val = this.contactForm.getRawValue();

    this.maintenanceService.submitTicket(id, {
      propertyAddress: val.propertyAddress!,
      tenantPhone:     val.tenantPhone!,
      tenantEmail:     val.tenantEmail!,
    }).subscribe({
      next: () => {
        this.pollingMessage.set('Ticket submitted! AI is evaluating your request…');
        this.startPolling(id);
      },
      error: () => {
        this.submitting.set(false);
        this.toast.error('Failed to submit ticket. Please try again.');
      },
    });
  }

  private startPolling(id: string): void {
    this.pollRetries = 0;
    this.pollInterval = setInterval(() => {
      this.maintenanceService.getTicketDetail(id).subscribe({
        next: ticket => {
          this.pollRetries++;
          if (ticket.status !== 'UnderAiReview' || this.pollRetries >= 5) {
            this.clearPoll();
            this.submittedTicketId.set(id);
            this.submitSuccess.set(true);
            this.submitting.set(false);
          }
        },
        error: () => {
          this.clearPoll();
          this.submittedTicketId.set(id);
          this.submitSuccess.set(true);
          this.submitting.set(false);
        },
      });
    }, 3000);
  }

  private clearPoll(): void {
    if (this.pollInterval !== null) {
      clearInterval(this.pollInterval);
      this.pollInterval = null;
    }
  }

  viewTicket(): void {
    const id = this.submittedTicketId();
    if (id) this.router.navigate(['/dashboard/maintenance-requests', id]);
    else this.router.navigate(['/dashboard/maintenance-requests']);
  }

  goBackToList(): void {
    this.router.navigate(['/dashboard/maintenance-requests']);
  }

  readonly priorities: TicketPriority[] = ['Low', 'Medium', 'Emergency'];

  setPriority(p: TicketPriority): void {
    this.basicForm.patchValue({ priority: p });
  }

  priorityIcon(p: string | null | undefined): string {
    const map: Record<string, string> = {
      Low: 'fa-circle-check', Medium: 'fa-circle-exclamation', Emergency: 'fa-triangle-exclamation',
    };
    return map[p ?? ''] ?? 'fa-circle-check';
  }

  priorityColor(p: string | null | undefined): string {
    const map: Record<string, string> = {
      Low: '#16a34a', Medium: '#b45309', Emergency: '#dc2626',
    };
    return map[p ?? ''] ?? '#16a34a';
  }
}
