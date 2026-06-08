import { Component, inject } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { InViewDirective } from '../../../../shared/directives/in-view.directive';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink, InViewDirective],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
  private fb = inject(FormBuilder);

  form = this.fb.group({
    email:    ['', [Validators.required, Validators.email, Validators.pattern(/^[^\s@]+@[^\s@]+\.[^\s@]+$/)]],
    password: ['', [Validators.required, Validators.minLength(8)]],
  });

  showPassword = false;
  loading = false;

  get email()    { return this.form.get('email')!; }
  get password() { return this.form.get('password')!; }

  onSubmit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.loading = true;
    // TODO: wire up auth service
    console.log('Login:', this.form.value);
  }
}
