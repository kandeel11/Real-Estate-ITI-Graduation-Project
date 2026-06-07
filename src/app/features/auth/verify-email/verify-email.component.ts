/**
 * Verify Email Component
 * Component for email verification after registration
 */

import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-verify-email',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="verify-email">
      <h2>Verify Your Email</h2>
      <p>We've sent a verification link to your email address.</p>
      <p class="instruction">
        Please check your email and click the link to verify your account.
      </p>
      <div class="actions">
        <a href="/auth/login" class="btn-primary">Back to Login</a>
        <button class="btn-secondary">Resend Email</button>
      </div>
    </div>
  `,
  styles: [
    `
      .verify-email {
        text-align: center;
      }

      .verify-email h2 {
        color: #2c3e50;
        margin-bottom: 1rem;
      }

      .verify-email p {
        color: #6c757d;
        margin-bottom: 1rem;
      }

      .instruction {
        background-color: #e8f4f8;
        padding: 1rem;
        border-radius: 4px;
        border-left: 4px solid #3498db;
        margin: 1.5rem 0 !important;
      }

      .actions {
        display: flex;
        gap: 1rem;
        justify-content: center;
        margin-top: 2rem;
      }

      .btn-primary,
      .btn-secondary {
        padding: 0.75rem 1.5rem;
        border-radius: 4px;
        text-decoration: none;
        cursor: pointer;
        border: none;
        font-size: 1rem;
        transition: background-color 0.3s;
      }

      .btn-primary {
        background-color: #3498db;
        color: white;
      }

      .btn-primary:hover {
        background-color: #2980b9;
      }

      .btn-secondary {
        background-color: #95a5a6;
        color: white;
      }

      .btn-secondary:hover {
        background-color: #7f8c8d;
      }
    `,
  ],
})
export class VerifyEmailComponent {}
