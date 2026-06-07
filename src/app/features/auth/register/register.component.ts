/**
 * Register Component
 * Standalone auth component for user registration
 */

import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="register-form">
      <h2>Create Account</h2>
      <p class="subtitle">Join our real estate platform</p>
      <form [formGroup]="registerForm" (ngSubmit)="onSubmit()">
        <div class="form-row">
          <div class="form-group">
            <label for="firstName">First Name</label>
            <input
              type="text"
              id="firstName"
              formControlName="firstName"
              placeholder="First name"
            />
          </div>
          <div class="form-group">
            <label for="lastName">Last Name</label>
            <input
              type="text"
              id="lastName"
              formControlName="lastName"
              placeholder="Last name"
            />
          </div>
        </div>

        <div class="form-group">
          <label for="email">Email</label>
          <input
            type="email"
            id="email"
            formControlName="email"
            placeholder="Enter your email"
          />
        </div>

        <div class="form-group">
          <label for="password">Password</label>
          <input
            type="password"
            id="password"
            formControlName="password"
            placeholder="Enter your password"
          />
        </div>

        <div class="form-group">
          <label for="confirmPassword">Confirm Password</label>
          <input
            type="password"
            id="confirmPassword"
            formControlName="confirmPassword"
            placeholder="Confirm your password"
          />
        </div>

        <div class="form-group">
          <label for="role">Register as</label>
          <select id="role" formControlName="role">
            <option value="">Select your role</option>
            <option value="company">Company</option>
            <option value="broker">Broker</option>
            <option value="tenant">Tenant</option>
            <option value="maintenance-company">Maintenance Company</option>
          </select>
        </div>

        <button type="submit" [disabled]="!registerForm.valid">
          Create Account
        </button>

        <p class="login-link">
          Already have an account? <a href="/auth/login">Login</a>
        </p>
      </form>
    </div>
  `,
  styles: [
    `
      .register-form {
        width: 100%;
      }

      .register-form h2 {
        margin-bottom: 0.5rem;
        color: #2c3e50;
      }

      .subtitle {
        margin: 0 0 1.5rem 0;
        color: #95a5a6;
        font-size: 0.875rem;
      }

      .form-row {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 1rem;
      }

      .form-group {
        margin-bottom: 1rem;
      }

      .form-group label {
        display: block;
        margin-bottom: 0.5rem;
        color: #2c3e50;
        font-weight: 500;
        font-size: 0.875rem;
      }

      .form-group input,
      .form-group select {
        width: 100%;
        padding: 0.75rem;
        border: 1px solid #bdc3c7;
        border-radius: 4px;
        font-size: 1rem;
        box-sizing: border-box;
      }

      .form-group input:focus,
      .form-group select:focus {
        outline: none;
        border-color: #3498db;
        box-shadow: 0 0 0 3px rgba(52, 152, 219, 0.1);
      }

      button {
        width: 100%;
        padding: 0.75rem;
        background-color: #3498db;
        color: white;
        border: none;
        border-radius: 4px;
        font-size: 1rem;
        font-weight: 500;
        cursor: pointer;
        transition: background-color 0.3s;
      }

      button:hover:not(:disabled) {
        background-color: #2980b9;
      }

      button:disabled {
        background-color: #95a5a6;
        cursor: not-allowed;
      }

      .login-link {
        margin-top: 1rem;
        text-align: center;
        color: #6c757d;
        font-size: 0.875rem;
      }

      .login-link a {
        color: #3498db;
        text-decoration: none;
      }

      .login-link a:hover {
        text-decoration: underline;
      }

      @media (max-width: 600px) {
        .form-row {
          grid-template-columns: 1fr;
        }
      }
    `,
  ],
})
export class RegisterComponent {
  registerForm: FormGroup;

  constructor(private fb: FormBuilder) {
    this.registerForm = this.fb.group({
      firstName: ['', [Validators.required]],
      lastName: ['', [Validators.required]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', [Validators.required]],
      role: ['', [Validators.required]],
    });
  }

  onSubmit(): void {
    if (this.registerForm.invalid) return;
    console.log('Registration data:', this.registerForm.value);
    // TODO: Implement registration logic
  }
}
