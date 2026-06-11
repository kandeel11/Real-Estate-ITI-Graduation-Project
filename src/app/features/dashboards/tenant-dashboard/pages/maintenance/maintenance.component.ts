import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ReactiveFormsModule, FormsModule, FormBuilder, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { MaintenanceService, Ticket, TicketDetail, TicketPriority } from '../../services/maintenance.service';


@Component({
  selector: 'app-maintenance',
  standalone: true,
  imports: [DatePipe, ReactiveFormsModule, FormsModule],
  templateUrl: './maintenance.component.html',
  styleUrl: './maintenance.component.css',
})
export class MaintenanceComponent implements OnInit {
  private readonly maintenanceService = inject(MaintenanceService);
  private readonly fb = inject(FormBuilder);

  protected activeTab = signal<'tickets' | 'create'>('tickets');
  protected loading = signal(true);
  protected error = signal<string | null>(null);
  protected tickets = signal<Ticket[]>([]);
  protected selectedTicket = signal<TicketDetail | null>(null);
  protected detailLoading = signal(false);
  protected newComment = '';
  protected commentLoading = signal(false);

  // Create-ticket state
  protected createLoading = signal(false);
  protected createSuccess = signal(false);
  protected uploadedFiles = signal<File[]>([]);
  protected previewUrls = signal<string[]>([]);
  protected dragOver = signal(false);

  readonly createForm = this.fb.group({
    title:       ['', [Validators.required, Validators.minLength(5)]],
    description: ['', [Validators.required, Validators.minLength(20)]],
    priority:    ['Low' as TicketPriority, Validators.required],
    propertyId:  [{ value: 'prop-001', disabled: true }],
  });

  protected readonly ticketStats = computed(() => {
    const list = this.tickets();
    return {
      total:              list.length,
      open:               list.filter(t => t.status === 'Open').length,
      inProgress:         list.filter(t => t.status === 'InProgress').length,
      waitingForDecision: list.filter(t => t.status === 'WaitingForDecision' || t.status === 'WaitingForTenantDecision').length,
      completed:          list.filter(t => t.status === 'Completed').length,
    };
  });

  ngOnInit(): void {
    this.loadTickets();
  }

  loadTickets(): void {
    this.loading.set(true);
    this.error.set(null);
    this.maintenanceService.getTickets().pipe(finalize(() => this.loading.set(false))).subscribe({
      next: data => this.tickets.set(data),
      error: () => this.error.set('Failed to load tickets. Please try again.'),
    });
  }

  openDetail(ticket: Ticket): void {
    this.selectedTicket.set(null);
    this.detailLoading.set(true);
    this.maintenanceService.getTicketDetail(ticket.id).pipe(finalize(() => this.detailLoading.set(false))).subscribe({
      next: d => this.selectedTicket.set(d),
    });
  }

  closeDetail(): void {
    this.selectedTicket.set(null);
  }

  makeDecision(ticketId: string, decision: 'platform' | 'external'): void {
    this.maintenanceService.makeDecision(ticketId, decision).subscribe({
      next: () => {
        this.closeDetail();
        this.loadTickets();
      },
    });
  }

  submitComment(): void {
    const t = this.selectedTicket();
    if (!t || !this.newComment.trim()) return;
    this.commentLoading.set(true);
    this.maintenanceService.addComment(t.id, this.newComment.trim())
      .pipe(finalize(() => this.commentLoading.set(false)))
      .subscribe({
        next: comment => {
          this.selectedTicket.update(cur => cur ? { ...cur, comments: [...cur.comments, comment] } : cur);
          this.newComment = '';
        },
      });
  }

  onFileSelect(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files) this.addFiles(Array.from(input.files));
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.dragOver.set(false);
    if (event.dataTransfer?.files) this.addFiles(Array.from(event.dataTransfer.files));
  }

  private addFiles(files: File[]): void {
    const images = files.filter(f => f.type.startsWith('image/'));
    this.uploadedFiles.update(cur => [...cur, ...images]);
    images.forEach(f => {
      const reader = new FileReader();
      reader.onload = e => this.previewUrls.update(cur => [...cur, e.target!.result as string]);
      reader.readAsDataURL(f);
    });
  }

  removeFile(index: number): void {
    this.uploadedFiles.update(cur => cur.filter((_, i) => i !== index));
    this.previewUrls.update(cur => cur.filter((_, i) => i !== index));
  }

  submitTicket(): void {
    if (this.createForm.invalid) return;
    this.createLoading.set(true);
    const val = this.createForm.getRawValue();

    this.maintenanceService.createTicket({
      title:       val.title!,
      description: val.description!,
      priority:    val.priority as TicketPriority,
      propertyId:  val.propertyId!,
    }).subscribe({
      next: ({ id }) => {
        const uploads = this.uploadedFiles().map(f => this.maintenanceService.uploadAttachment(id, f));
        const proceed = () => {
          this.maintenanceService.submitTicket(id, {
            propertyAddress: '42 Oak Street, Suite 3B, New York, NY 10001',
            tenantPhone: '+1 (212) 555-0142',
          }).pipe(finalize(() => this.createLoading.set(false))).subscribe({
            next: () => {
              this.createSuccess.set(true);
              this.createForm.reset({ priority: 'Low', propertyId: 'prop-001' });
              this.uploadedFiles.set([]);
              this.previewUrls.set([]);
              this.loadTickets();
            },
          });
        };
        if (uploads.length === 0) { proceed(); return; }
        let done = 0;
        uploads.forEach(obs => obs.subscribe({ next: () => { if (++done === uploads.length) proceed(); } }));
      },
      error: () => {
        this.createLoading.set(false);
        this.error.set('Failed to create ticket.');
      },
    });
  }

  priorityClass(p: TicketPriority): string {
    return { Emergency: 'priority-emergency', Medium: 'priority-medium', Low: 'priority-low' }[p];
  }

  statusClass(s: string): string {
    const map: Record<string, string> = {
      Open: 'status-open', InProgress: 'status-progress',
      WaitingForDecision: 'status-waiting', WaitingForTenantDecision: 'status-waiting',
      Completed: 'status-done', Cancelled: 'status-cancelled',
    };
    return map[s] ?? 'status-open';
  }

  statusLabel(s: string): string {
    const map: Record<string, string> = {
      WaitingForDecision: 'Awaiting Decision', WaitingForTenantDecision: 'Your Decision Needed',
    };
    return map[s] ?? s;
  }

  timeAgo(dateStr: string): string {
    const s = Math.floor((Date.now() - new Date(dateStr).getTime()) / 1000);
    if (s < 60) return 'just now';
    const m = Math.floor(s / 60);
    if (m < 60) return `${m}m ago`;
    const h = Math.floor(m / 60);
    if (h < 24) return `${h}h ago`;
    return `${Math.floor(h / 24)}d ago`;
  }
}
