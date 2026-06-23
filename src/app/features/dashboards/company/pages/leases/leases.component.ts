import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { finalize } from 'rxjs/operators';
import {
  CompanyService,
  PendingLeaseApproval,
  AwaitingTenantLease,
} from '../../services/company.service';

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

  /** Leases waiting for manager review. */
  readonly pending = signal<PendingLeaseApproval[]>([]);

  /** Leases the manager approved — now waiting for tenant to accept. */
  readonly awaitingTenant = signal<AwaitingTenantLease[]>([]);

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
    forkJoin({
      pending:        this.svc.getPendingApprovals(),
      awaitingTenant: this.svc.getAwaitingTenantLeases(),
    }).pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: ({ pending, awaitingTenant }) => {
          this.pending.set(pending);
          this.awaitingTenant.set(awaitingTenant);
        },
        error: () => this.error.set('Could not load lease data. Please try again.'),
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
          // Reload awaiting-tenant list so the newly approved lease appears there.
          if (isApproved) {
            this.svc.getAwaitingTenantLeases().subscribe({
              next: list => this.awaitingTenant.set(list),
            });
          }
        },
        error: (err) =>
          this.actionError.set(
            err?.error?.error ?? (isApproved ? 'Could not approve.' : 'Could not reject.'),
          ),
      });
  }
}
