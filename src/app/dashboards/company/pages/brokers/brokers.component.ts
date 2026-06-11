/**
 * Company Brokers Page
 * Team member list with deactivate and an add-broker modal that surfaces
 * the subscription broker-limit error with an upgrade link.
 * Matches backend contract: POST /companies/mine/brokers requires a
 * tempPassword; deactivation is permanent (blocks the broker's login).
 */

import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { CompanyService } from '../../services/company.service';
import { CompanySubscriptionService } from '../../services/company-subscription.service';
import { Broker } from '../../../../shared/models/company.model';
import { ConfirmModalComponent } from '../../../../shared/components/modal/confirm-modal.component';

@Component({
  selector: 'app-company-brokers',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, ConfirmModalComponent],
  templateUrl: './brokers.component.html',
  styleUrl: './brokers.component.css',
})
export class BrokersComponent implements OnInit {
  private companySvc = inject(CompanyService);
  private subSvc = inject(CompanySubscriptionService);
  private fb = inject(FormBuilder);

  brokers = signal<Broker[]>([]);
  loading = signal(true);
  showModal = signal(false);
  submitting = signal(false);
  errorMsg = signal<string | null>(null);

  private subscription = toSignal(this.subSvc.getActive());

  activeCount = computed(() => this.brokers().filter((b) => b.isActive).length);

  /** Plan's broker limit — '∞' for unlimited, '…' while loading. */
  maxBrokersLabel = computed(() => {
    const max = this.subscription()?.maxBrokers;
    if (max == null) return '…';
    return max >= 999999 ? '∞' : String(max);
  });

  form = this.fb.nonNullable.group({
    fullName: ['', [Validators.required, Validators.minLength(3)]],
    email: ['', [Validators.required, Validators.email]],
    tempPassword: ['', [Validators.required, Validators.minLength(8)]],
    phone: ['', Validators.required],
    licenseNumber: ['', Validators.required],
  });

  get fullName() { return this.form.get('fullName')!; }
  get email() { return this.form.get('email')!; }
  get tempPassword() { return this.form.get('tempPassword')!; }
  get phone() { return this.form.get('phone')!; }
  get licenseNumber() { return this.form.get('licenseNumber')!; }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.companySvc.getBrokers().subscribe({
      next: (brokers) => {
        this.brokers.set(brokers);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  openModal(): void {
    this.errorMsg.set(null);
    this.form.reset();
    this.showModal.set(true);
  }

  closeModal(): void {
    this.showModal.set(false);
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitting.set(true);
    this.errorMsg.set(null);

    this.companySvc.addBroker(this.form.getRawValue()).subscribe({
      next: () => {
        this.submitting.set(false);
        this.showModal.set(false);
        this.form.reset();
        this.load();
      },
      error: (err) => {
        this.submitting.set(false);
        // Backend error shape: { error: "message" }
        this.errorMsg.set(
          err?.error?.error ?? err?.error?.message ?? 'Failed to add broker'
        );
      },
    });
  }

  brokerToDeactivate = signal<Broker | null>(null);
  deactivating = signal(false);
  actionError = signal<string | null>(null);

  deactivateMessage = computed(() => {
    const broker = this.brokerToDeactivate();
    return broker
      ? `Deactivate ${this.displayName(broker)}? They will no longer be able to log in.`
      : '';
  });

  deactivate(broker: Broker): void {
    this.brokerToDeactivate.set(broker);
  }

  confirmDeactivate(): void {
    const broker = this.brokerToDeactivate();
    if (!broker || this.deactivating()) return;
    this.deactivating.set(true);
    this.companySvc.deactivateBroker(broker.id).subscribe({
      next: () => {
        this.deactivating.set(false);
        this.brokerToDeactivate.set(null);
        this.load();
      },
      error: () => {
        this.deactivating.set(false);
        this.brokerToDeactivate.set(null);
      },
    });
  }

  // ── Reactivate ──────────────────────────────────────────────────
  reactivatingId = signal<string | null>(null);

  reactivate(broker: Broker): void {
    if (this.reactivatingId()) return;
    this.reactivatingId.set(broker.id);
    this.actionError.set(null);
    this.companySvc.reactivateBroker(broker.id).subscribe({
      next: () => {
        this.reactivatingId.set(null);
        this.load();
      },
      error: (err) => {
        this.reactivatingId.set(null);
        // e.g. broker limit reached — backend shape: { error: "message" }
        this.actionError.set(
          err?.error?.error ??
            (err?.status === 404
              ? 'Reactivate endpoint not found — restart the backend to pick up the latest build.'
              : `Failed to reactivate the broker (HTTP ${err?.status ?? '?'}).`)
        );
      },
    });
  }

  // ── Permanent delete ────────────────────────────────────────────
  brokerToDelete = signal<Broker | null>(null);
  deleting = signal(false);

  deleteMessage = computed(() => {
    const broker = this.brokerToDelete();
    return broker
      ? `Permanently delete ${this.displayName(broker)}? Their listings stay with the company — reassign them to another broker from the Properties page.`
      : '';
  });

  requestDelete(broker: Broker): void {
    this.brokerToDelete.set(broker);
  }

  confirmDelete(): void {
    const broker = this.brokerToDelete();
    if (!broker || this.deleting()) return;
    this.deleting.set(true);
    this.actionError.set(null);
    this.companySvc.deleteBroker(broker.id).subscribe({
      next: () => {
        this.deleting.set(false);
        this.brokerToDelete.set(null);
        this.load();
      },
      error: (err) => {
        this.deleting.set(false);
        this.brokerToDelete.set(null);
        this.actionError.set(err?.error?.error ?? 'Failed to delete the broker.');
      },
    });
  }

  displayName(broker: Broker): string {
    return broker.fullName || `License ${broker.licenseNumber}`;
  }
}
