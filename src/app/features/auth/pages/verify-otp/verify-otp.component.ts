import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { finalize, interval, Subscription, takeWhile } from 'rxjs';
import { TranslatePipe } from '@ngx-translate/core';
import { environment } from '../../../../../environments/environment';
import { API_ENDPOINTS } from '../../../../core/constants/api-endpoints';

@Component({
  selector: 'app-verify-otp',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe],
  templateUrl: './verify-otp.component.html',
  styleUrl: './verify-otp.component.css',
})
export class VerifyOtpComponent implements OnInit, OnDestroy {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly http = inject(HttpClient);

  email = this.route.snapshot.queryParamMap.get('email') ?? '';

  form = this.fb.group({
    code: ['', [Validators.required, Validators.pattern(/^\d{6}$/)]],
  });

  loading = false;
  resending = false;
  errorMsg: string | null = null;
  successMsg: string | null = null;
  resendCooldown = 0;

  private cooldownSub?: Subscription;

  get code() { return this.form.get('code')!; }

  ngOnInit(): void {
    if (!this.email) {
      this.router.navigate(['/auth/login']);
    }
  }

  ngOnDestroy(): void {
    this.cooldownSub?.unsubscribe();
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.errorMsg = null;

    this.http
      .post<{ success: boolean; message: string }>(
        `${environment.apiUrl}${API_ENDPOINTS.AUTH.VERIFY_OTP}`,
        { email: this.email, code: this.code.value }
      )
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (res) => {
          if (res.success) {
            this.router.navigate(['/auth/login'], { queryParams: { verified: 1 } });
          } else {
            this.errorMsg = res.message || 'Invalid or expired code. Please try again.';
          }
        },
        error: (err) => {
          this.errorMsg =
            err?.error?.message ?? err?.error?.error ?? 'Verification failed. Please try again.';
        },
      });
  }

  resend(): void {
    if (this.resendCooldown > 0 || this.resending) return;

    this.resending = true;
    this.errorMsg = null;
    this.successMsg = null;

    this.http
      .post<{ success: boolean; message: string; expiresInSeconds: number }>(
        `${environment.apiUrl}${API_ENDPOINTS.AUTH.SEND_OTP}`,
        { email: this.email }
      )
      .pipe(finalize(() => (this.resending = false)))
      .subscribe({
        next: () => {
          this.successMsg = 'A new code has been sent to your email.';
          this.startCooldown(60);
        },
        error: (err) => {
          this.errorMsg =
            err?.error?.message ?? err?.error?.error ?? 'Could not resend the code. Try again shortly.';
        },
      });
  }

  private startCooldown(seconds: number): void {
    this.resendCooldown = seconds;
    this.cooldownSub?.unsubscribe();
    this.cooldownSub = interval(1000)
      .pipe(takeWhile(() => this.resendCooldown > 0))
      .subscribe(() => this.resendCooldown--);
  }
}
