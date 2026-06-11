import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl } from '@angular/forms';
import { finalize } from 'rxjs';
import { ProfileService, UserProfile } from '../../services/profile.service';

function passwordStrength(password: string): number {
  if (!password) return 0;
  let score = 0;
  if (password.length >= 8)  score++;
  if (password.length >= 12) score++;
  if (/[A-Z]/.test(password)) score++;
  if (/[0-9]/.test(password)) score++;
  if (/[^A-Za-z0-9]/.test(password)) score++;
  return score;
}

function confirmPasswordValidator(control: AbstractControl): { [key: string]: boolean } | null {
  const parent = control.parent;
  if (!parent) return null;
  const newPw = parent.get('newPassword')?.value;
  return control.value !== newPw ? { mismatch: true } : null;
}

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.css',
})
export class ProfileComponent implements OnInit {
  private readonly profileService = inject(ProfileService);
  private readonly fb = inject(FormBuilder);

  protected activeSection = signal<'info' | 'password' | 'danger'>('info');
  protected loading = signal(true);
  protected profile = signal<UserProfile | null>(null);
  protected saveLoading = signal(false);
  protected saveSuccess = signal(false);
  protected pwLoading = signal(false);
  protected pwSuccess = signal(false);
  protected pwError = signal<string | null>(null);
  protected showCurrentPw = signal(false);
  protected showNewPw = signal(false);

  readonly profileForm = this.fb.group({
    firstName: ['', [Validators.required, Validators.minLength(2)]],
    lastName:  ['', [Validators.required, Validators.minLength(2)]],
    phone:     [''],
  });

  readonly passwordForm = this.fb.group({
    currentPassword: ['', Validators.required],
    newPassword:     ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', [Validators.required, confirmPasswordValidator]],
  });

  protected readonly pwStrength = computed(() => {
    const pw = this.passwordForm.get('newPassword')?.value ?? '';
    return passwordStrength(pw);
  });

  get pwStrengthLabel(): string {
    const s = this.pwStrength();
    if (s <= 1) return 'Weak';
    if (s <= 3) return 'Fair';
    if (s <= 4) return 'Good';
    return 'Strong';
  }

  get pwStrengthClass(): string {
    const s = this.pwStrength();
    if (s <= 1) return 'strength-weak';
    if (s <= 3) return 'strength-fair';
    if (s <= 4) return 'strength-good';
    return 'strength-strong';
  }

  get initials(): string {
    const p = this.profile();
    if (!p) return 'T';
    return `${p.firstName.charAt(0)}${p.lastName.charAt(0)}`.toUpperCase();
  }

  ngOnInit(): void {
    this.loadProfile();
  }

  loadProfile(): void {
    this.loading.set(true);
    this.profileService.getProfile().pipe(finalize(() => this.loading.set(false))).subscribe({
      next: p => {
        this.profile.set(p);
        this.profileForm.patchValue({ firstName: p.firstName, lastName: p.lastName, phone: p.phone });
      },
    });
  }

  saveProfile(): void {
    if (this.profileForm.invalid) return;
    this.saveLoading.set(true);
    this.saveSuccess.set(false);
    const val = this.profileForm.getRawValue();
    this.profileService.updateProfile({ firstName: val.firstName!, lastName: val.lastName!, phone: val.phone ?? '' })
      .pipe(finalize(() => this.saveLoading.set(false)))
      .subscribe({
        next: updated => {
          this.profile.set(updated);
          this.saveSuccess.set(true);
          setTimeout(() => this.saveSuccess.set(false), 3000);
        },
      });
  }

  changePassword(): void {
    if (this.passwordForm.invalid) return;
    this.pwLoading.set(true);
    this.pwError.set(null);
    const val = this.passwordForm.getRawValue();
    this.profileService.changePassword({ currentPassword: val.currentPassword!, newPassword: val.newPassword! })
      .pipe(finalize(() => this.pwLoading.set(false)))
      .subscribe({
        next: () => {
          this.pwSuccess.set(true);
          this.passwordForm.reset();
          setTimeout(() => this.pwSuccess.set(false), 3000);
        },
        error: () => this.pwError.set('Current password is incorrect or the change failed. Please try again.'),
      });
  }
}
