import { Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../../../core/services/auth.service';
import { InViewDirective } from '../../../../shared/directives/in-view.directive';

type Step = 'pick-role' | 'individual' | 'company';
type IndividualRole = 'owner' | 'broker' | 'tenant';

function pwMatch(group: AbstractControl) {
  const pw = group.get('password')?.value;
  const cf = group.get('confirm')?.value;
  return pw && cf && pw !== cf ? { passwordMismatch: true } : null;
}

const ROLE_CARDS = [
  {
    value: 'tenant' as const,
    label: 'Tenant',
    subtitle: 'Looking for a property to rent',
    icon: 'fa-solid fa-key',
    gradient: 'linear-gradient(135deg,#6366f1,#8b5cf6)',
  },
  {
    value: 'owner' as const,
    label: 'Property Owner',
    subtitle: 'List and manage your properties',
    icon: 'fa-solid fa-house-chimney',
    gradient: 'linear-gradient(135deg,#059669,#34d399)',
  },
  {
    value: 'broker' as const,
    label: 'Broker',
    subtitle: 'Connect buyers, sellers & tenants',
    icon: 'fa-solid fa-handshake',
    gradient: 'linear-gradient(135deg,#f59e0b,#fbbf24)',
  },
  {
    value: 'company' as const,
    label: 'Real Estate Company',
    subtitle: 'Register your company on Estatein',
    icon: 'fa-solid fa-building',
    gradient: 'linear-gradient(135deg,#3b82f6,#06b6d4)',
  },
];

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, InViewDirective],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss',
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);

  readonly roleCards = ROLE_CARDS;

  step = signal<Step>('pick-role');
  chosenRole = signal<IndividualRole | 'company' | null>(null);
  loading = signal(false);
  errorMsg = signal<string | null>(null);

  showPw = false;
  showConfirmPw = false;
  showCoPw = false;
  showCoConfirmPw = false;

  // ── Individual form ──────────────────────────────────────────
  iForm = this.fb.group(
    {
      fullName: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]],
      phone: ['', [Validators.required, Validators.pattern(/^\+?[\d\s\-()٠-٩]{7,15}$/)]],
      nationalIdNumber: ['', [Validators.required, Validators.minLength(5)]],
      password: ['', [Validators.required, Validators.minLength(8)]],
      confirm: ['', Validators.required],
    },
    { validators: pwMatch },
  );

  // ── Company form ─────────────────────────────────────────────
  cForm = this.fb.group(
    {
      adminFullName: ['', [Validators.required, Validators.minLength(2)]],
      adminEmail: ['', [Validators.required, Validators.email]],
      adminPhone: ['', [Validators.required, Validators.pattern(/^\+?[\d\s\-()٠-٩]{7,15}$/)]],
      password: ['', [Validators.required, Validators.minLength(8)]],
      confirm: ['', Validators.required],
      companyName: ['', [Validators.required, Validators.minLength(2)]],
      commercialRegister: ['', [Validators.required, Validators.minLength(3)]],
      city: ['', Validators.required],
      address: ['', Validators.required],
      companyPhone: ['', [Validators.required, Validators.pattern(/^\+?[\d\s\-()٠-٩]{7,15}$/)]],
      companyEmail: ['', [Validators.required, Validators.email]],
    },
    { validators: pwMatch },
  );

  // ── Navigation ───────────────────────────────────────────────
  selectRole(role: IndividualRole | 'company'): void {
    this.chosenRole.set(role);
    this.errorMsg.set(null);
    this.step.set(role === 'company' ? 'company' : 'individual');
  }

  back(): void {
    this.step.set('pick-role');
    this.errorMsg.set(null);
  }

  get selectedCard() {
    return this.roleCards.find((c) => c.value === this.chosenRole());
  }

  // ── Submit individual ────────────────────────────────────────
  submitIndividual(): void {
    if (this.iForm.invalid) {
      this.iForm.markAllAsTouched();
      return;
    }
    const v = this.iForm.getRawValue();
    this.loading.set(true);
    this.errorMsg.set(null);
    this.auth
      .register({
        fullName: v.fullName!,
        email: v.email!,
        phone: v.phone!,
        nationalIdNumber: v.nationalIdNumber!,
        role: this.chosenRole() as IndividualRole,
        password: v.password!,
      })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: () => this.router.navigate(['/auth/verify-otp'], { queryParams: { email: v.email } }),
        error: (err) =>
          this.errorMsg.set(
            err?.error?.error ?? err?.error?.message ?? 'Registration failed. Please try again.',
          ),
      });
  }

  // ── Submit company ───────────────────────────────────────────
  submitCompany(): void {
    if (this.cForm.invalid) {
      this.cForm.markAllAsTouched();
      return;
    }
    const v = this.cForm.getRawValue();
    this.loading.set(true);
    this.errorMsg.set(null);
    this.auth
      .registerCompany({
        adminFullName: v.adminFullName!,
        adminEmail: v.adminEmail!,
        adminPhone: v.adminPhone!,
        password: v.password!,
        companyName: v.companyName!,
        commercialRegister: v.commercialRegister!,
        city: v.city!,
        address: v.address!,
        companyPhone: v.companyPhone!,
        companyEmail: v.companyEmail!,
      })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: () =>
          this.router.navigate(['/auth/verify-otp'], { queryParams: { email: v.adminEmail } }),
        error: (err) =>
          this.errorMsg.set(
            err?.error?.error ??
              err?.error?.message ??
              'Company registration failed. Please try again.',
          ),
      });
  }

  // ── Individual getters ───────────────────────────────────────
  get iFullName() {
    return this.iForm.get('fullName')!;
  }
  get iEmail() {
    return this.iForm.get('email')!;
  }
  get iPhone() {
    return this.iForm.get('phone')!;
  }
  get iNatId() {
    return this.iForm.get('nationalIdNumber')!;
  }
  get iPassword() {
    return this.iForm.get('password')!;
  }
  get iConfirm() {
    return this.iForm.get('confirm')!;
  }

  // ── Company getters ──────────────────────────────────────────
  get cAdminName() {
    return this.cForm.get('adminFullName')!;
  }
  get cAdminEmail() {
    return this.cForm.get('adminEmail')!;
  }
  get cAdminPhone() {
    return this.cForm.get('adminPhone')!;
  }
  get cPassword() {
    return this.cForm.get('password')!;
  }
  get cConfirm() {
    return this.cForm.get('confirm')!;
  }
  get cName() {
    return this.cForm.get('companyName')!;
  }
  get cRegister() {
    return this.cForm.get('commercialRegister')!;
  }
  get cCity() {
    return this.cForm.get('city')!;
  }
  get cAddress() {
    return this.cForm.get('address')!;
  }
  get cPhone() {
    return this.cForm.get('companyPhone')!;
  }
  get cEmail() {
    return this.cForm.get('companyEmail')!;
  }
}
