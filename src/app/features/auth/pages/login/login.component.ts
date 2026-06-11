import { Component, inject } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../../../core/services/auth.service';
import { InViewDirective } from '../../../../shared/directives/in-view.directive';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink, InViewDirective],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  form = this.fb.group({
    email:    ['', [Validators.required, Validators.email, Validators.pattern(/^[^\s@]+@[^\s@]+\.[^\s@]+$/)]],
    password: ['', [Validators.required, Validators.minLength(8)]],
  });

  showPassword = false;
  loading = false;
  serverError: string | null = null;

  get email()    { return this.form.get('email')!; }
  get password() { return this.form.get('password')!; }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.serverError = null;

    const { email, password } = this.form.value;
    this.authService.login(email!, password!)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: res => {
          this.authService.setUser(res.user, res.accessToken, res.refreshToken);
          this.router.navigate(['/dashboard']);
        },
        error: () => {
          this.serverError = 'Invalid email or password. Please try again.';
        },
      });
  }
}
