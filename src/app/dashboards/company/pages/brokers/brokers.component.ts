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
import { CompanyService } from '../../services/company.service';
import { Broker } from '../../../../shared/models/company.model';

const MAX_BROKERS = 25;

@Component({
  selector: 'app-company-brokers',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './brokers.component.html',
  styleUrl: './brokers.component.css',
})
export class BrokersComponent implements OnInit {
  private companySvc = inject(CompanyService);
  private fb = inject(FormBuilder);

  brokers = signal<Broker[]>([]);
  loading = signal(true);
  showModal = signal(false);
  submitting = signal(false);
  errorMsg = signal<string | null>(null);

  activeCount = computed(() => this.brokers().filter((b) => b.isActive).length);
  maxBrokers = MAX_BROKERS;

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

  deactivate(broker: Broker): void {
    if (!confirm(`Deactivate ${this.displayName(broker)}? They will no longer be able to log in.`)) return;
    this.companySvc.deactivateBroker(broker.id).subscribe(() => this.load());
  }

  displayName(broker: Broker): string {
    return broker.fullName ?? `License ${broker.licenseNumber}`;
  }
}
