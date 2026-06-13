/**
 * Company Settings Page
 * Two sections: the COMPANY profile (PUT /companies/mine — commercial
 * register locked after verification, logo preview-only) and the manager's
 * own ACCOUNT (PUT /users/me/profile + /users/me/password).
 */

import { Component, inject, signal, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { CompanyService } from '../../services/company.service';
import { Company } from '../../../../shared/models/company.model';
import {
  UserProfileService,
  UserProfile,
} from '../../../../core/services/user-profile.service';

@Component({
  selector: 'app-company-settings',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.css',
})
export class SettingsComponent implements OnInit {
  private companySvc = inject(CompanyService);
  private profileSvc = inject(UserProfileService);
  private fb = inject(FormBuilder);

  company = signal<Company | null>(null);
  loading = signal(true);
  saving = signal(false);
  saved = signal(false);
  logoPreview = signal<string | null>(null);

  /** Active settings tab. */
  tab = signal<'company' | 'account' | 'password'>('company');

  // ── Company profile ─────────────────────────────────────────────
  // email/commercialRegister are not updatable via PUT /companies/mine
  form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.minLength(3)]],
    website: [''],
    description: [''],
    city: ['', Validators.required],
    address: ['', Validators.required],
    phone: ['', Validators.required],
    email: [{ value: '', disabled: true }],
    commercialRegister: [{ value: '', disabled: true }],
  });

  get name() { return this.form.get('name')!; }
  get city() { return this.form.get('city')!; }
  get address() { return this.form.get('address')!; }
  get phone() { return this.form.get('phone')!; }

  // ── Manager account ─────────────────────────────────────────────
  profile = signal<UserProfile | null>(null);
  accountSaving = signal(false);
  accountSaved = signal(false);
  accountError = signal<string | null>(null);

  accountForm = this.fb.nonNullable.group({
    fullName: ['', [Validators.required, Validators.minLength(3)]],
    phone: ['', Validators.required],
    email: [{ value: '', disabled: true }],
  });

  get accountFullName() { return this.accountForm.get('fullName')!; }
  get accountPhone() { return this.accountForm.get('phone')!; }

  passwordSaving = signal(false);
  passwordSaved = signal(false);
  passwordError = signal<string | null>(null);

  passwordForm = this.fb.nonNullable.group({
    currentPassword: ['', Validators.required],
    newPassword: ['', [Validators.required, Validators.minLength(8)]],
    confirmNewPassword: ['', Validators.required],
  });

  get currentPassword() { return this.passwordForm.get('currentPassword')!; }
  get newPassword() { return this.passwordForm.get('newPassword')!; }
  get confirmNewPassword() { return this.passwordForm.get('confirmNewPassword')!; }

  ngOnInit(): void {
    this.companySvc.getMyCompany().subscribe((company) => {
      this.company.set(company);
      this.logoPreview.set(company.logoUrl ?? null);
      this.form.patchValue({
        name: company.name,
        website: company.website ?? '',
        description: company.description ?? '',
        city: company.city,
        address: company.address,
        phone: company.phone,
        email: company.email,
        commercialRegister: company.commercialRegister,
      });
      this.loading.set(false);
    });

    this.profileSvc.getMe().subscribe({
      next: (profile) => {
        this.profile.set(profile);
        this.accountForm.patchValue({
          fullName: profile.fullName,
          phone: profile.phone,
          email: profile.email,
        });
      },
      error: () => this.accountError.set('Could not load your account details.'),
    });
  }

  logoUploading = signal(false);
  logoError = signal<string | null>(null);

  onLogoSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;

    // Instant local preview while the upload runs
    const reader = new FileReader();
    reader.onload = () => this.logoPreview.set(reader.result as string);
    reader.readAsDataURL(file);

    this.logoUploading.set(true);
    this.logoError.set(null);

    this.companySvc.uploadLogo(file).subscribe({
      next: ({ logoUrl }) => {
        this.logoUploading.set(false);
        this.logoPreview.set(logoUrl);
        const company = this.company();
        if (company) this.company.set({ ...company, logoUrl });
      },
      error: (err) => {
        this.logoUploading.set(false);
        this.logoError.set(err?.error?.error ?? 'Failed to upload the logo.');
        this.logoPreview.set(this.company()?.logoUrl ?? null);
      },
    });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const company = this.company();
    if (!company) return;

    this.saving.set(true);
    this.saved.set(false);

    // Backend body: { name, city, address, phone, website?, description?, logoUrl? }
    // logoUrl must be echoed back — the backend overwrites it with whatever is
    // sent, so omitting it would wipe an existing logo
    const { name, city, address, phone, website, description } = this.form.getRawValue();
    const changes = { name, city, address, phone, website, description, logoUrl: company.logoUrl ?? null };

    this.companySvc.updateCompany(changes).subscribe({
      next: () => {
        this.company.set({ ...company, ...changes });
        this.saving.set(false);
        this.saved.set(true);
        setTimeout(() => this.saved.set(false), 4000);
      },
      error: () => this.saving.set(false),
    });
  }

  saveAccount(): void {
    if (this.accountForm.invalid) {
      this.accountForm.markAllAsTouched();
      return;
    }
    this.accountSaving.set(true);
    this.accountSaved.set(false);
    this.accountError.set(null);

    const { fullName, phone } = this.accountForm.getRawValue();
    this.profileSvc.updateProfile({ fullName, phone }).subscribe({
      next: () => {
        this.accountSaving.set(false);
        this.accountSaved.set(true);
        setTimeout(() => this.accountSaved.set(false), 4000);
      },
      error: (err) => {
        this.accountSaving.set(false);
        this.accountError.set(err?.error?.error ?? 'Failed to save your account details.');
      },
    });
  }

  changePassword(): void {
    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }
    const { currentPassword, newPassword, confirmNewPassword } = this.passwordForm.getRawValue();
    if (newPassword !== confirmNewPassword) {
      this.passwordError.set('New password and confirmation do not match.');
      return;
    }

    this.passwordSaving.set(true);
    this.passwordSaved.set(false);
    this.passwordError.set(null);

    this.profileSvc
      .changePassword({ currentPassword, newPassword, confirmNewPassword })
      .subscribe({
        next: () => {
          this.passwordSaving.set(false);
          this.passwordSaved.set(true);
          this.passwordForm.reset();
          setTimeout(() => this.passwordSaved.set(false), 4000);
        },
        error: (err) => {
          this.passwordSaving.set(false);
          this.passwordError.set(err?.error?.error ?? 'Failed to change the password.');
        },
      });
  }
}
