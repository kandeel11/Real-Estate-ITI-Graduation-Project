import { Component, inject } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { InViewDirective } from '../../../../shared/directives/in-view.directive';
import { AuthService } from '../../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink, InViewDirective],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

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

  onSubmit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.loading = true;
    this.errorMsg = null;
    this.sessionExpired = false;

    const { email, password } = this.form.getRawValue();
    this.auth.login(email!, password!).subscribe({
      next: (user) => {
        const home = this.auth.dashboardRouteFor(user.role);
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
        // Only honor returnUrl if it's internal and the role can actually open it,
        // otherwise the guard would bounce straight back to login.
        const target =
          returnUrl?.startsWith('/') &&
          (!returnUrl.startsWith('/dashboard/') || returnUrl.startsWith(home.replace(/\/overview$/, '')))
            ? returnUrl
            : home;
        this.router.navigateByUrl(target);
      },
      error: (err) => {
        this.loading = false;
        // Backend error shape: { error: "message" }
        this.errorMsg = err?.error?.error ?? 'Sign-in failed. Please check your connection and try again.';
      },
    });
  }
}
