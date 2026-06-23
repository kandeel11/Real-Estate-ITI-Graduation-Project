import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CompanyService, PendingLeaseApproval } from '../../services/company.service';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-company-leases',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './leases.component.html',
  styleUrl: './leases.component.css',
})
export class LeasesComponent implements OnInit {
  private readonly svc = inject(CompanyService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly pending = signal<PendingLeaseApproval[]>([]);

  /** Which lease's approval dialog is open. */
  readonly reviewingId = signal<string | null>(null);
  readonly actionInProgress = signal(false);
  readonly actionError = signal<string | null>(null);
  reviewNotes = '';

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.svc.getPendingApprovals().subscribe({
      next:  items => { this.pending.set(items); this.loading.set(false); },
      error: ()    => { this.error.set('Could not load pending approvals.'); this.loading.set(false); },
    });
  }

  openReview(lease: PendingLeaseApproval): void {
    this.reviewingId.set(lease.id);
    this.reviewNotes = '';
    this.actionError.set(null);
  }

  closeReview(): void {
    this.reviewingId.set(null);
  }

  reviewing(): PendingLeaseApproval | null {
    const id = this.reviewingId();
    return id ? (this.pending().find(l => l.id === id) ?? null) : null;
  }

  decide(isApproved: boolean): void {
    const id = this.reviewingId();
    if (!id || this.actionInProgress()) return;
    this.actionInProgress.set(true);
    this.actionError.set(null);
    this.svc
      .approveLease(id, isApproved, this.reviewNotes || undefined)
      .pipe(finalize(() => this.actionInProgress.set(false)))
      .subscribe({
        next: () => {
          this.pending.update(list => list.filter(l => l.id !== id));
          this.reviewingId.set(null);
        },
        error: (err) =>
          this.actionError.set(err?.error?.error ?? (isApproved ? 'Could not approve.' : 'Could not reject.')),
      });
  }
}
