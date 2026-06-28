import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-rent-payment-cancelled',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="result-page">
      <div class="result-card">
        <div class="result-icon">
          <i class="fa-solid fa-circle-xmark"></i>
        </div>
        <h1 class="result-title">Payment Cancelled</h1>
        <p class="result-desc">
          You cancelled the payment process. Your rent payment has not been charged.
          You can return to your payments and try again at any time.
        </p>
        <div class="result-actions">
          <a routerLink="/dashboard/payments" class="btn-primary-link">
            <i class="fa-solid fa-arrow-left"></i> Back to Payments
          </a>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .result-page {
      display: flex;
      align-items: center;
      justify-content: center;
      min-height: 70vh;
      padding: 2rem;
    }
    .result-card {
      background: var(--color-surface-container-lowest, #fff);
      border: 1px solid var(--color-outline-variant, #e5e7eb);
      border-radius: 20px;
      padding: 3.5rem 2.5rem;
      text-align: center;
      max-width: 460px;
      width: 100%;
      box-shadow: 0 4px 24px rgba(0,0,0,.06);
    }
    .result-icon {
      font-size: 4rem;
      margin-bottom: 1.25rem;
      line-height: 1;
      color: #94a3b8;
    }
    .result-title {
      font-size: 1.6rem;
      font-weight: 800;
      margin: 0 0 12px;
      color: var(--color-on-surface, #0f172a);
    }
    .result-desc {
      font-size: 14.5px;
      color: var(--color-on-surface-variant, #64748b);
      margin: 0 0 24px;
      line-height: 1.7;
    }
    .result-actions {
      display: flex;
      justify-content: center;
    }
    .btn-primary-link {
      display: inline-flex;
      align-items: center;
      gap: 8px;
      background: #4f46e5;
      color: #fff;
      border-radius: 10px;
      padding: 11px 24px;
      font-size: 14px;
      font-weight: 700;
      text-decoration: none;
      transition: opacity .15s;
      &:hover { opacity: .88; }
    }
  `],
})
export class RentPaymentCancelledComponent {}
