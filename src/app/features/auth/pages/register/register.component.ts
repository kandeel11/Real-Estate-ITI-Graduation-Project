import { Component, inject } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { finalize } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { API_ENDPOINTS } from '../../../../core/constants/api-endpoints';

const ROLES = [
  { value: 'owner',  label: 'Property Owner' },
  { value: 'broker', label: 'Individual Broker' },
  { value: 'tenant', label: 'Tenant' },
] as const;

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './register.component.css',
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly http = inject(HttpClient);

  readonly roles = ROLES;

  form = this.fb.group({
    fullName: ['', [Validators.required, Validators.minLength(2)]],
    email:    ['', [Validators.required, Validators.email]],
    phone:    ['', [Validators.required, Validators.pattern(/^\+?[\d\s\-()]{7,15}$/)]],
    role:     ['tenant', Validators.required],
    password: ['', [Validators.required, Validators.minLength(8)]],
    confirm:  ['', Validators.required],
  }, { validators: this.passwordsMatch });

  showPassword = false;
  showConfirm = false;
  loading = false;
  errorMsg: string | null = null;

  get fullName() { return this.form.get('fullName')!; }
  get email()    { return this.form.get('email')!; }
  get phone()    { return this.form.get('phone')!; }
  get role()     { return this.form.get('role')!; }
  get password() { return this.form.get('password')!; }
  get confirm()  { return this.form.get('confirm')!; }

  private passwordsMatch(group: AbstractControl) {
    const pw = group.get('password')?.value;
    const cf = group.get('confirm')?.value;
    return pw && cf && pw !== cf ? { passwordMismatch: true } : null;
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.errorMsg = null;

    const { fullName, email, phone, role, password } = this.form.getRawValue();

    this.http
      .post(`${environment.apiUrl}${API_ENDPOINTS.AUTH.REGISTER}`, {
        fullName,
        email,
        phone,
        role,
        password,
      })
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: () => {
          this.router.navigate(['/auth/verify-otp'], { queryParams: { email } });
        },
        error: (err) => {
          this.errorMsg =
            err?.error?.error ?? err?.error?.message ?? 'Registration failed. Please try again.';
        },
      });
  }
}
