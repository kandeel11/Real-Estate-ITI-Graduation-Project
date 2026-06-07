/**
 * Verify OTP Component
 * Component for OTP verification
 */

import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-verify-otp',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="verify-otp">
      <h2>Enter OTP</h2>
      <p>We've sent a one-time password to your registered email.</p>
      <!-- OTP input implementation -->
      <p class="placeholder">OTP Verification Component Placeholder</p>
    </div>
  `,
  styles: [
    `
      .verify-otp {
        text-align: center;
      }
      .verify-otp h2 {
        color: #2c3e50;
      }
      .placeholder {
        color: #95a5a6;
        font-style: italic;
      }
    `,
  ],
})
export class VerifyOtpComponent {}
