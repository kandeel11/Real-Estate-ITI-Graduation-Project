import { Component, inject } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { InViewDirective } from '../../../../shared/directives/in-view.directive';
import { AuthService } from '../../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink, InViewDirective],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  form = this.fb.group({
    email:    ['', [Validators.required, Validators.email, Validators.pattern(/^[^\s@]+@[^\s@]+\.[^\s@]+$/)]],
    password: ['', [Validators.required, Validators.minLength(8)]],
  });

  showPassword = false;
  loading = false;
  errorMsg: string | null = null;
  sessionExpired = this.route.snapshot.queryParamMap.has('expired');

  get email()    { return this.form.get('email')!; }
  get password() { return this.form.get('password')!; }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.errorMsg = null;
    this.sessionExpired = false;

    const { email, password } = this.form.getRawValue();
    // AuthService.login stores the session and maps the backend role
    // (company_manager → company) before emitting the signed-in user
    this.auth.login(email!, password!)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (user) => {
          const home = this.auth.dashboardRouteFor(user.role);
          const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
          // Only honor returnUrl if it's internal and the role can actually
          // open it, otherwise the guard would bounce straight back to login.
          const target =
            returnUrl?.startsWith('/') &&
            (!returnUrl.startsWith('/dashboard/') || returnUrl.startsWith(home.replace(/\/overview$/, '')))
              ? returnUrl
              : home;
          this.router.navigateByUrl(target);
        },
        error: (err) => {
          // Backend error shape: { error: "message" }
          this.errorMsg = err?.error?.error ?? 'Sign-in failed. Please check your connection and try again.';
        },
      });
  }
}
