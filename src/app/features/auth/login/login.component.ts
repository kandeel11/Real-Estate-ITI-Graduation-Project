/**
 * Login Component
 * Standalone auth component for user login
 */

import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="login-form">
      <h2>Login to Your Account</h2>
      <form [formGroup]="loginForm" (ngSubmit)="onSubmit()">
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

        <button type="submit" [disabled]="!loginForm.valid || isLoading">
          {{ isLoading ? 'Logging in...' : 'Login' }}
        </button>

        <p class="signup-link">
          Don't have an account? <a href="/auth/register">Sign up</a>
        </p>
      </form>
    </div>
  `,
  styles: [
    `
      .login-form {
        width: 100%;
      }

      .login-form h2 {
        margin-bottom: 1.5rem;
        color: #2c3e50;
      }

      .form-group {
        margin-bottom: 1rem;
      }

      .form-group label {
        display: block;
        margin-bottom: 0.5rem;
        color: #2c3e50;
        font-weight: 500;
      }

      .form-group input {
        width: 100%;
        padding: 0.75rem;
        border: 1px solid #bdc3c7;
        border-radius: 4px;
        font-size: 1rem;
        box-sizing: border-box;
      }

      .form-group input:focus {
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

      .signup-link {
        margin-top: 1rem;
        text-align: center;
        color: #6c757d;
        font-size: 0.875rem;
      }

      .signup-link a {
        color: #3498db;
        text-decoration: none;
      }

      .signup-link a:hover {
        text-decoration: underline;
      }
    `,
  ],
})
export class LoginComponent {
  loginForm: FormGroup;
  isLoading = false;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
    });
  }

  onSubmit(): void {
    if (this.loginForm.invalid) return;

    this.isLoading = true;
    const { email, password } = this.loginForm.value;

    this.authService.login(email, password).subscribe({
      next: (response) => {
        this.authService.setUser(response.user, response.token);
        this.router.navigate([`/dashboard/${response.user.role}/overview`]);
      },
      error: (error) => {
        this.isLoading = false;
        console.error('Login failed', error);
        alert('Invalid credentials');
      },
    });
  }
}
