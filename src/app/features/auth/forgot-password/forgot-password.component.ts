/**
 * Forgot Password Component
 * Component for password reset request
 */

import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="forgot-password">
      <h2>Forgot Password?</h2>
      <p>Enter your email address and we'll send you a link to reset your password.</p>
      <!-- Password reset form implementation -->
      <p class="placeholder">Forgot Password Component Placeholder</p>
    </div>
  `,
  styles: [
    `
      .forgot-password {
        text-align: center;
      }
      .forgot-password h2 {
        color: #2c3e50;
      }
      .placeholder {
        color: #95a5a6;
        font-style: italic;
      }
    `,
  ],
})
export class ForgotPasswordComponent {}
