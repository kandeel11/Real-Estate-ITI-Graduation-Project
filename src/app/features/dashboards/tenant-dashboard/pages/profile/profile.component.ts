import { Component, OnInit, OnDestroy, inject, signal, computed } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl } from '@angular/forms';
import { finalize } from 'rxjs';
import { ProfileService, UserProfile } from '../../services/profile.service';
import { ToastService } from '../../../../../shared/components/toast/toast.service';

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
export class ProfileComponent implements OnInit, OnDestroy {
  private readonly profileService = inject(ProfileService);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(FormBuilder);

  protected activeSection = signal<'info' | 'password' | 'danger'>('info');
  protected loading = signal(true);
  protected error = signal<string | null>(null);
  protected profile = signal<UserProfile | null>(null);

  protected saveLoading = signal(false);

  protected pwLoading = signal(false);
  protected pwSuccess = signal(false);
  protected pwError = signal<string | null>(null);

  protected showCurrentPw = signal(false);
  protected showNewPw = signal(false);

  protected verificationLoading = signal(false);
  protected verificationSent = signal(false);
  protected verificationError = signal<string | null>(null);
  protected verificationCountdown = signal(0);
  private _countdownInterval: ReturnType<typeof setInterval> | null = null;

  /** Set of field names currently unlocked for editing */
  protected readonly editingFields = signal(new Set<string>());

  isEditing(field: string): boolean {
    return this.editingFields().has(field);
  }

  toggleEdit(field: string): void {
    this.editingFields.update(set => {
      const next = new Set(set);
      if (next.has(field)) { next.delete(field); } else { next.add(field); }
      return next;
    });
  }

  readonly profileForm = this.fb.group({
    fullName: ['', [Validators.required, Validators.minLength(2)]],
    email:    ['', [Validators.required, Validators.email]],
    phone:    [''],
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
    const name = this.profile()?.fullName ?? '';
    return name.split(' ').slice(0, 2).map(w => w.charAt(0)).join('').toUpperCase() || 'T';
  }

  ngOnInit(): void {
    this.loadProfile();
  }

  loadProfile(): void {
    this.loading.set(true);
    this.error.set(null);
    this.profileService.getProfile()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: p => {
          this.profile.set(p);
          this.profileForm.patchValue({ fullName: p.fullName, email: p.email, phone: p.phone ?? '' });
        },
        error: () => this.error.set('Failed to load profile. Please try again.'),
      });
  }

  saveProfile(): void {
    if (this.profileForm.invalid) return;
    this.saveLoading.set(true);
    const val = this.profileForm.getRawValue();
    const dto = { fullName: val.fullName!, email: val.email!, phone: val.phone ?? '' };
    this.profileService.updateProfile(dto)
      .pipe(finalize(() => this.saveLoading.set(false)))
      .subscribe({
        next: () => {
          // Update signal manually — don't rely on API response shape
          this.profile.update(p => p ? { ...p, ...dto } : p);
          this.editingFields.set(new Set());
          this.toast.success('Profile updated successfully.');
        },
        error: () => this.toast.error('Failed to save profile. Please try again.'),
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

  sendVerification(): void {
    const email = this.profile()?.email;
    if (!email || this.verificationLoading() || this.verificationCountdown() > 0) return;
    this.verificationLoading.set(true);
    this.verificationError.set(null);
    this.profileService.resendVerification(email)
      .pipe(finalize(() => this.verificationLoading.set(false)))
      .subscribe({
        next: () => {
          this.verificationSent.set(true);
          this.startCountdown();
        },
        error: () => this.verificationError.set('Failed to send verification email. Please try again.'),
      });
  }

  private startCountdown(): void {
    if (this._countdownInterval) clearInterval(this._countdownInterval);
    this.verificationCountdown.set(60);
    this._countdownInterval = setInterval(() => {
      this.verificationCountdown.update(n => {
        if (n <= 1) {
          clearInterval(this._countdownInterval!);
          this._countdownInterval = null;
          this.verificationSent.set(false);
          return 0;
        }
        return n - 1;
      });
    }, 1000);
  }

  ngOnDestroy(): void {
    if (this._countdownInterval) clearInterval(this._countdownInterval);
  }
}
