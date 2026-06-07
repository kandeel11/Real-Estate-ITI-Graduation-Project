/**
 * Authentication Layout Component
 * Wrapper for auth-related routes (login, register, OTP verification, password reset)
 * Centered, minimal design for authentication forms
 */

import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-auth-layout',
  standalone: true,
  imports: [RouterOutlet, CommonModule],
  template: `
    <div class="auth-layout">
      <div class="auth-container">
        <!-- Logo/Brand -->
        <div class="auth-brand">
          <h1>RealEstate</h1>
          <p>Property Management Platform</p>
        </div>

        <!-- Auth Forms Outlet -->
        <div class="auth-form-container">
          <router-outlet />
        </div>

        <!-- Footer Links -->
        <div class="auth-footer">
          <p>
            &copy; 2026 RealEstate Platform. All rights reserved.
          </p>
        </div>
      </div>
    </div>
  `,
  styles: [
    `
      .auth-layout {
        display: flex;
        align-items: center;
        justify-content: center;
        min-height: 100vh;
        background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      }

      .auth-container {
        background: white;
        border-radius: 8px;
        box-shadow: 0 10px 40px rgba(0, 0, 0, 0.1);
        padding: 2rem;
        width: 100%;
        max-width: 400px;
      }

      .auth-brand {
        text-align: center;
        margin-bottom: 2rem;
      }

      .auth-brand h1 {
        margin: 0;
        color: #667eea;
        font-size: 2rem;
      }

      .auth-brand p {
        margin: 0.5rem 0 0 0;
        color: #6c757d;
        font-size: 0.875rem;
      }

      .auth-form-container {
        margin: 2rem 0;
      }

      .auth-footer {
        text-align: center;
        margin-top: 2rem;
        padding-top: 2rem;
        border-top: 1px solid #dee2e6;
        color: #6c757d;
        font-size: 0.875rem;
      }

      .auth-footer p {
        margin: 0;
      }

      @media (max-width: 600px) {
        .auth-container {
          padding: 1.5rem;
        }

        .auth-brand h1 {
          font-size: 1.5rem;
        }
      }
    `,
  ],
})
export class AuthLayoutComponent {}
