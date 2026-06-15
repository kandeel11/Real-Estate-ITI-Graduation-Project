import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DecimalPipe } from '@angular/common';
import {
  MaintenanceCompanyProfile,
  MaintenanceProfileService,
  UpdateProfileRequest,
} from '../../services/maintenance-profile.service';

@Component({
  selector: 'app-maintenance-profile',
  standalone: true,
  imports: [FormsModule, DecimalPipe],
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MaintenanceProfileComponent implements OnInit {
  private readonly profileService = inject(MaintenanceProfileService);
  private readonly cdr = inject(ChangeDetectorRef);

  loading = signal(true);
  saving = signal(false);
  editMode = signal(false);
  toast = signal<{ msg: string; type: 'success' | 'error' } | null>(null);
  profile = signal<MaintenanceCompanyProfile | null>(null);

  form = {
    companyName: '',
    phone: '',
    city: '',
    serviceRadiusKm: 0,
    description: '',
    logoUrl: '',
  };

  ngOnInit(): void {
    this.loadProfile();
  }

  private loadProfile(): void {
    this.loading.set(true);
    this.profileService.getProfile().subscribe({
      next: (p) => {
        this.profile.set(p);
        this.fillForm(p);
        this.loading.set(false);
        this.cdr.markForCheck();
      },
      error: () => {
        this.loading.set(false);
        this.showToast('Failed to load profile. Please try again.', 'error');
        this.cdr.markForCheck();
      },
    });
  }

  private fillForm(p: MaintenanceCompanyProfile): void {
    this.form.companyName = p.companyName;
    this.form.phone = p.phone;
    this.form.city = p.city;
    this.form.serviceRadiusKm = p.serviceRadiusKm;
    this.form.description = p.description ?? '';
    this.form.logoUrl = p.logoUrl ?? '';
  }

  startEdit(): void {
    const p = this.profile();
    if (p) this.fillForm(p);
    this.editMode.set(true);
  }

  cancelEdit(): void {
    this.editMode.set(false);
  }

  saveProfile(): void {
    const p = this.profile();
    if (!p) return;
    this.saving.set(true);

    const body: UpdateProfileRequest = {
      companyName: this.form.companyName,
      phone: this.form.phone,
      city: this.form.city,
      serviceRadiusKm: this.form.serviceRadiusKm,
      description: this.form.description,
      logoUrl: this.form.logoUrl || null,
      categoryIds: p.categories.map((c) => c.id),
    };

    this.profileService.updateProfile(body).subscribe({
      next: (updated) => {
        this.profile.set(updated);
        this.saving.set(false);
        this.editMode.set(false);
        this.showToast('Profile updated successfully!', 'success');
        this.cdr.markForCheck();
      },
      error: () => {
        this.saving.set(false);
        this.showToast('Failed to save profile. Please try again.', 'error');
        this.cdr.markForCheck();
      },
    });
  }

  private showToast(msg: string, type: 'success' | 'error'): void {
    this.toast.set({ msg, type });
    setTimeout(() => {
      this.toast.set(null);
      this.cdr.markForCheck();
    }, 3500);
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
  }

  get initials(): string {
    const name = this.profile()?.companyName ?? '';
    return name
      .split(' ')
      .slice(0, 2)
      .map((w) => (w[0] ?? '').toUpperCase())
      .join('');
  }
}
