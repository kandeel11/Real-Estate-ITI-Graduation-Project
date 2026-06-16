import {
  AfterViewInit,
  Component,
  ElementRef,
  inject,
  OnDestroy,
  OnInit,
  QueryList,
  ViewChildren,
} from '@angular/core';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { finalize, interval, Subscription, takeWhile } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { API_ENDPOINTS } from '../../../../core/constants/api-endpoints';

@Component({
  selector: 'app-verify-otp',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './verify-otp.component.html',
  styleUrl: './verify-otp.component.css',
})
export class VerifyOtpComponent implements OnInit, AfterViewInit, OnDestroy {
  private readonly router   = inject(Router);
  private readonly route    = inject(ActivatedRoute);
  private readonly http     = inject(HttpClient);

  @ViewChildren('digitInput') digitInputs!: QueryList<ElementRef<HTMLInputElement>>;

  email       = this.route.snapshot.queryParamMap.get('email') ?? '';
  maskedEmail = '';

  digits = ['', '', '', '', '', ''];

  loading       = false;
  resending     = false;
  errorMsg:  string | null = null;
  successMsg: string | null = null;
  resendCooldown = 0;
  verified       = false;

  private cooldownSub?: Subscription;

  get code(): string { return this.digits.join(''); }

  get canSubmit(): boolean { return /^\d{6}$/.test(this.code); }

  ngOnInit(): void {
    if (!this.email) { this.router.navigate(['/auth/login']); return; }
    this.maskedEmail = this.maskEmail(this.email);
    this.sendOtp(true);   // ← auto-send OTP immediately on arrival
  }

  ngAfterViewInit(): void {
    setTimeout(() => this.digitInputs?.first?.nativeElement.focus(), 400);
  }

  ngOnDestroy(): void { this.cooldownSub?.unsubscribe(); }

  // ── Input handlers ───────────────────────────────────────────────────────────

  onDigitInput(index: number, event: Event): void {
    const input = event.target as HTMLInputElement;
    const value = input.value.replace(/\D/g, '').slice(-1);
    this.digits[index] = value;
    input.value        = value;
    this.errorMsg      = null;

    if (value && index < 5) {
      this.digitInputs.toArray()[index + 1].nativeElement.focus();
    }
  }

  onKeydown(index: number, event: KeyboardEvent): void {
    if (event.key === 'Backspace') {
      if (!this.digits[index] && index > 0) {
        this.digits[index - 1] = '';
        this.digitInputs.toArray()[index - 1].nativeElement.focus();
      } else {
        this.digits[index] = '';
        (event.target as HTMLInputElement).value = '';
      }
    }
  }

  onPaste(event: ClipboardEvent): void {
    event.preventDefault();
    const pasted = (event.clipboardData?.getData('text') ?? '').replace(/\D/g, '').slice(0, 6);
    pasted.split('').forEach((ch, i) => {
      this.digits[i] = ch;
      this.digitInputs.toArray()[i].nativeElement.value = ch;
    });
    const lastFilled = Math.min(pasted.length, 5);
    setTimeout(() => this.digitInputs.toArray()[lastFilled].nativeElement.focus());
  }

  onFocus(event: FocusEvent): void {
    (event.target as HTMLInputElement).select();
  }

  // ── Submit ───────────────────────────────────────────────────────────────────

  onSubmit(): void {
    if (!this.canSubmit) return;
    this.loading  = true;
    this.errorMsg = null;

    this.http
      .post<{ success: boolean; message: string }>(
        `${environment.apiUrl}${API_ENDPOINTS.AUTH.VERIFY_OTP}`,
        { email: this.email, code: this.code },
      )
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (res) => {
          if (res.success) {
            this.verified = true;
            setTimeout(() => this.router.navigate(['/auth/login'], { queryParams: { verified: 1 } }), 2000);
          } else {
            this.errorMsg = res.message || 'Invalid or expired code. Please try again.';
            this.shakeDigits();
          }
        },
        error: (err) => {
          this.errorMsg = err?.error?.message ?? err?.error?.error ?? 'Verification failed. Please try again.';
          this.shakeDigits();
        },
      });
  }

  // ── Resend ───────────────────────────────────────────────────────────────────

  resend(): void {
    if (this.resendCooldown > 0 || this.resending) return;
    this.sendOtp(false);
  }

  private sendOtp(isInitial: boolean): void {
    this.resending  = true;
    this.errorMsg   = null;
    this.successMsg = null;

    this.http
      .post<{ success: boolean; message: string }>(
        `${environment.apiUrl}${API_ENDPOINTS.AUTH.SEND_OTP}`,
        { email: this.email },
      )
      .pipe(finalize(() => (this.resending = false)))
      .subscribe({
        next: () => {
          if (!isInitial) this.successMsg = 'A new code has been sent to your email.';
          this.startCooldown(60);
        },
        error: (err) => {
          this.errorMsg = err?.error?.message ?? err?.error?.error ?? 'Could not send the code. Try again shortly.';
        },
      });
  }

  // ── Helpers ──────────────────────────────────────────────────────────────────

  private maskEmail(email: string): string {
    const [local, domain] = email.split('@');
    if (!domain) return email;
    const visible = local.slice(0, 2);
    const stars   = '*'.repeat(Math.max(local.length - 2, 3));
    return `${visible}${stars}@${domain}`;
  }

  private shakeDigits(): void {
    this.digitInputs?.forEach((el) => {
      el.nativeElement.classList.add('shake');
      setTimeout(() => el.nativeElement.classList.remove('shake'), 600);
    });
  }

  private startCooldown(seconds: number): void {
    this.resendCooldown = seconds;
    this.cooldownSub?.unsubscribe();
    this.cooldownSub = interval(1000)
      .pipe(takeWhile(() => this.resendCooldown > 0))
      .subscribe(() => this.resendCooldown--);
  }

  /** SVG countdown ring: circle circumference = 2π×15 ≈ 94.25 */
  get ringOffset(): number {
    return 94.25 * (1 - this.resendCooldown / 60);
  }
}
