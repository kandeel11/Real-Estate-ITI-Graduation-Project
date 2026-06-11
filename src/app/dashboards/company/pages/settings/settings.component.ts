/**
 * Company Settings Page
 * Edit company profile. Commercial register is locked after verification.
 * Logo upload is preview-only until a real upload endpoint exists.
 */

import { Component, inject, signal, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { CompanyService } from '../../services/company.service';
import { Company } from '../../../../shared/models/company.model';

@Component({
  selector: 'app-company-settings',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.css',
})
export class SettingsComponent implements OnInit {
  private companySvc = inject(CompanyService);
  private fb = inject(FormBuilder);

  company = signal<Company | null>(null);
  loading = signal(true);
  saving = signal(false);
  saved = signal(false);
  logoPreview = signal<string | null>(null);

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
  }

  onLogoSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    const reader = new FileReader();
    reader.onload = () => this.logoPreview.set(reader.result as string);
    reader.readAsDataURL(file);
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
}
