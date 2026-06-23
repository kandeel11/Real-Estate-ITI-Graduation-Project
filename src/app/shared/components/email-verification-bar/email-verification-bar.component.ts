/**
 * Global Email Verification Bar
 * Sticky top bar shown on every page while a signed-in user hasn't verified
 * their email. Lets them resend the verification link (POST /auth/resend-verification)
 * with a 60s cooldown. Rendered once at the app root, above the router-outlet.
 *
 * Verification happens in a separate browser tab (the emailed link hits the API),
 * so the SPA's access token still says email_verified=false afterwards. To clear
 * the bar — and reopen the server-side gates, which read the JWT claim — we force a
 * token refresh when the user returns to the tab (and via an "I've verified" link).
 */

import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { finalize } from 'rxjs';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../../core/services/auth.service';

const REFRESH_THROTTLE_MS = 15_000;

@Component({
  selector: 'app-email-verification-bar',
  standalone: true,
  imports: [TranslatePipe],
  templateUrl: './email-verification-bar.component.html',
  styleUrl: './email-verification-bar.component.css',
})
export class EmailVerificationBarComponent implements OnInit, OnDestroy {
  private readonly auth = inject(AuthService);

  /** Visible only for an authenticated user whose email is not yet verified. */
  readonly show = computed(() => {
    const user = this.auth.user();
    return this.auth.authenticated() && !!user && user.isEmailVerified === false;
  });

  readonly email = computed(() => this.auth.user()?.email ?? '');

  readonly loading = signal(false);
  readonly sent = signal(false);
  readonly error = signal<string | null>(null);
  readonly countdown = signal(0);
  readonly checking = signal(false);

  private _interval: ReturnType<typeof setInterval> | null = null;
  private _lastRefresh = 0;
  private readonly _onVisible = () => {
    if (document.visibilityState === 'visible') this.checkVerification();
  };

  ngOnInit(): void {
    // When the user comes back from the email tab, pull a fresh token so the bar
    // clears (and gates reopen) without a manual re-login.
    document.addEventListener('visibilitychange', this._onVisible);
    window.addEventListener('focus', this._onVisible);
  }

  /**
   * Force a token refresh to pick up a freshly-verified status.
   * Auto triggers (tab focus) are throttled; a manual click (force) is not.
   */
  checkVerification(force = false): void {
    if (!this.show() || this.checking()) return;
    const now = Date.now();
    if (!force && now - this._lastRefresh < REFRESH_THROTTLE_MS) return;
    this._lastRefresh = now;

    this.checking.set(true);
    this.auth.refresh()
      .pipe(finalize(() => this.checking.set(false)))
      // Swallow errors: a failed refresh is handled by AuthService (it clears the
      // session); returning to the tab shouldn't surface an error here.
      .subscribe({ error: () => {} });
  }

  send(): void {
    const email = this.email();
    if (!email || this.loading() || this.countdown() > 0) return;
    this.loading.set(true);
    this.error.set(null);
    this.auth.resendVerification(email)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: () => {
          this.sent.set(true);
          this.startCountdown();
        },
        error: () => this.error.set('Could not send the verification email. Please try again.'),
      });
  }

  private startCountdown(): void {
    if (this._interval) clearInterval(this._interval);
    this.countdown.set(60);
    this._interval = setInterval(() => {
      this.countdown.update((n) => {
        if (n <= 1) {
          clearInterval(this._interval!);
          this._interval = null;
          this.sent.set(false);
          return 0;
        }
        return n - 1;
      });
    }, 1000);
  }

  ngOnDestroy(): void {
    if (this._interval) clearInterval(this._interval);
    document.removeEventListener('visibilitychange', this._onVisible);
    window.removeEventListener('focus', this._onVisible);
  }
}
