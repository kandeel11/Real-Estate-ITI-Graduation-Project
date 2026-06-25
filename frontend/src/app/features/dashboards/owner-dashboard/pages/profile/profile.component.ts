import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { finalize } from 'rxjs';
import { AuthService } from '../../../../../core/services/auth.service';
import { OwnerDataService } from '../../services/owner-data.service';
import { BrokerProfile } from '../../models/owner.models';
import { ToastService } from '../../../../../shared/components/toast/toast.service';

@Component({
  selector: 'app-owner-profile',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.css',
})
export class ProfileComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly dataService = inject(OwnerDataService);
  private readonly toast = inject(ToastService);

  /** Plain object so [(ngModel)] two-way binding keeps working. */
  profile: BrokerProfile | null = null;

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly saved = signal(false);

  /** Settings sub-tab: profile details vs. security/password. */
  readonly activeTab = signal<'profile' | 'security'>('profile');

  // Change password
  currentPassword = '';
  newPassword = '';
  confirmPassword = '';
  readonly changingPassword = signal(false);
  readonly passwordError = signal<string | null>(null);

  newLang = '';
  newSpec = '';

  get isCompanyBroker(): boolean { return !!this.auth.user()?.companyId; }

  ngOnInit(): void {
    this.dataService.getProfile().subscribe({
      next: p => { this.profile = { ...p }; this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  save(): void {
    if (!this.profile) return;
    this.saving.set(true);
    this.saved.set(false);
    setTimeout(() => { this.saving.set(false); this.saved.set(true); }, 800);
  }

  changePassword(): void {
    this.passwordError.set(null);
    if (!this.currentPassword || !this.newPassword) {
      this.passwordError.set('Please fill in all password fields.');
      return;
    }
    if (this.newPassword.length < 8) {
      this.passwordError.set('New password must be at least 8 characters.');
      return;
    }
    if (this.newPassword !== this.confirmPassword) {
      this.passwordError.set('New password and confirmation do not match.');
      return;
    }

    this.changingPassword.set(true);
    this.auth.changePassword(this.currentPassword, this.newPassword)
      .pipe(finalize(() => this.changingPassword.set(false)))
      .subscribe({
        next: () => {
          this.currentPassword = this.newPassword = this.confirmPassword = '';
          this.toast.success('Password changed successfully.');
        },
        error: (err) => {
          this.passwordError.set(err?.error?.error ?? 'Failed to change password. Check your current password.');
        },
      });
  }

  addLanguage(lang: string): void {
    if (lang.trim() && !this.profile!.languages.includes(lang.trim())) {
      this.profile!.languages = [...this.profile!.languages, lang.trim()];
    }
  }

  removeLanguage(lang: string): void {
    this.profile!.languages = this.profile!.languages.filter(l => l !== lang);
  }

  addSpec(spec: string): void {
    if (spec.trim() && !this.profile!.specializations.includes(spec.trim())) {
      this.profile!.specializations = [...this.profile!.specializations, spec.trim()];
    }
  }

  removeSpec(spec: string): void {
    this.profile!.specializations = this.profile!.specializations.filter(s => s !== spec);
  }
}
