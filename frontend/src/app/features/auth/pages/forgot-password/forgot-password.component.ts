/**
 * Forgot Password Page — two steps:
 *  1. email → POST /auth/forgot-password (sends a 6-digit reset code)
 *  2. code + new password → POST /auth/reset-password → back to login
 */

import { Component, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { API_ENDPOINTS } from '../../../../core/constants/api-endpoints';
import { InViewDirective } from '../../../../shared/directives/in-view.directive';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-forgot-password',
  imports: [ReactiveFormsModule, RouterLink, InViewDirective, TranslatePipe],
  templateUrl: './forgot-password.component.html',
  styleUrl: './forgot-password.component.css'
})
export class ForgotPasswordComponent {
  private readonly fb = inject(FormBuilder);
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  step = signal<1 | 2>(1);
  loading = signal(false);
  errorMsg = signal<string | null>(null);
  infoMsg = signal<string | null>(null);
  showPassword = false;

  emailForm = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
  });

  resetForm = this.fb.nonNullable.group({
    code: ['', [Validators.required, Validators.minLength(6), Validators.maxLength(6)]],
    newPassword: ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', Validators.required],
  });

  get email() { return this.emailForm.get('email')!; }
  get code() { return this.resetForm.get('code')!; }
  get newPassword() { return this.resetForm.get('newPassword')!; }
  get confirmPassword() { return this.resetForm.get('confirmPassword')!; }

  sendCode(): void {
    if (this.emailForm.invalid) {
      this.emailForm.markAllAsTouched();
      return;
    }
    this.loading.set(true);
    this.errorMsg.set(null);

    this.http
      .post<{ success: boolean; message: string }>(
        `${environment.apiUrl}${API_ENDPOINTS.AUTH.FORGOT_PASSWORD}`,
        { email: this.emailForm.getRawValue().email }
      )
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (res) => {
          this.infoMsg.set(res.message);
          this.step.set(2);
        },
        error: (err) =>
          this.errorMsg.set(err?.error?.error ?? 'Could not send the reset code. Please try again.'),
      });
  }

  resetPassword(): void {
    if (this.resetForm.invalid) {
      this.resetForm.markAllAsTouched();
      return;
    }
    const { code, newPassword, confirmPassword } = this.resetForm.getRawValue();
    if (newPassword !== confirmPassword) {
      this.errorMsg.set('Passwords do not match.');
      return;
    }

    this.loading.set(true);
    this.errorMsg.set(null);

    this.http
      .post(`${environment.apiUrl}${API_ENDPOINTS.AUTH.RESET_PASSWORD}`, {
        email: this.emailForm.getRawValue().email,
        code,
        newPassword,
      })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: () =>
          this.router.navigate(['/auth/login'], { queryParams: { reset: 1 } }),
        error: (err) =>
          this.errorMsg.set(err?.error?.error ?? 'Password reset failed. Please try again.'),
      });
  }
}
