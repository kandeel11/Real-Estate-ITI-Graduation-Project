import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../../../environments/environment';
import { API_ENDPOINTS } from '../../../../../core/constants/api-endpoints';
import { ToastService } from '../../../../../shared/components/toast/toast.service';

interface VerificationItem {
  verificationRequestId: string;
  status: string;
  aiConfidenceScore: number | null;
  aiOcrExtractedOwnerName: string | null;
  aiOcrExtractedIdExpiry: string | null;
  aiFakeDetectionFlag: boolean;
  adminReviewNotes: string | null;
  adminReviewedAt: string | null;
  rejectionReason: string | null;
  nationalIdDocumentUrl: string | null;
  ownershipProofDocumentUrl: string | null;
}

@Component({
  selector: 'app-ownership',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './ownership.component.html',
  styleUrl: './ownership.component.scss',
})
export class OwnershipComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly toast = inject(ToastService);

  items = signal<VerificationItem[]>([]);
  loading = signal(true);
  actionBusy = signal<string | null>(null);

  // modal state
  selected = signal<VerificationItem | null>(null);
  action = signal<'approve' | 'reject' | null>(null);
  notes = signal('');

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.http
      .get<VerificationItem[]>(
        `${environment.apiUrl}${API_ENDPOINTS.OWNERSHIP_VERIFICATIONS.PENDING_REVIEWS}`
      )
      .subscribe({
        next: (data) => { this.items.set(data); this.loading.set(false); },
        error: () => { this.loading.set(false); this.toast.error('Failed to load pending reviews.'); },
      });
  }

  openModal(item: VerificationItem, act: 'approve' | 'reject'): void {
    this.selected.set(item);
    this.action.set(act);
    this.notes.set('');
  }

  closeModal(): void {
    this.selected.set(null);
    this.action.set(null);
    this.notes.set('');
  }

  confirm(): void {
    const item = this.selected();
    const act = this.action();
    if (!item || !act || this.actionBusy()) return;

    const id = item.verificationRequestId;
    this.actionBusy.set(id);

    const url =
      act === 'approve'
        ? `${environment.apiUrl}${API_ENDPOINTS.OWNERSHIP_VERIFICATIONS.APPROVE(id)}`
        : `${environment.apiUrl}${API_ENDPOINTS.OWNERSHIP_VERIFICATIONS.REJECT(id)}`;

    this.http.post(url, { reviewNotes: this.notes() }).subscribe({
      next: () => {
        this.actionBusy.set(null);
        this.closeModal();
        this.toast.success(act === 'approve' ? 'Verification approved.' : 'Verification rejected.');
        this.load();
      },
      error: (err) => {
        this.actionBusy.set(null);
        this.toast.error(err?.error?.error ?? 'Action failed.');
      },
    });
  }

  statusClass(s: string): string {
    const m: Record<string, string> = {
      Pending: 'badge--pending',
      AiVerified: 'badge--verified',
      ManuallyVerified: 'badge--verified',
      RequiresManualReview: 'badge--review',
      Rejected: 'badge--rejected',
      Failed: 'badge--rejected',
    };
    return m[s] ?? 'badge--pending';
  }

  confidenceClass(score: number | null): string {
    if (score === null) return '';
    if (score >= 75) return 'score--high';
    if (score >= 50) return 'score--mid';
    return 'score--low';
  }
}
