import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { PaymentService } from '../../../../../../core/services/payment.service';

type ConfirmState = 'loading' | 'success' | 'error';

@Component({
  selector: 'app-rent-payment-success',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="result-page">
      <div class="result-card">

        @switch (state()) {

          @case ('loading') {
            <div class="result-icon loading">
              <i class="fa-solid fa-spinner fa-spin"></i>
            </div>
            <h1 class="result-title">Confirming Payment…</h1>
            <p class="result-desc">Please wait while we verify your payment with Stripe.</p>
          }

          @case ('success') {
            <div class="result-icon success">
              <i class="fa-solid fa-circle-check"></i>
            </div>
            <h1 class="result-title">Payment Successful!</h1>
            <p class="result-desc">
              Your rent payment has been confirmed and recorded.
              The payment will be credited to your landlord's wallet shortly.
            </p>
            @if (sessionId()) {
              <div class="session-ref">
                <span>Reference</span>
                <code>{{ sessionId() }}</code>
              </div>
            }
            <div class="result-actions">
              <a routerLink="/dashboard/payments" class="btn-primary-link">
                <i class="fa-solid fa-arrow-left"></i> Back to Payments
              </a>
            </div>
          }

          @case ('error') {
            <div class="result-icon error">
              <i class="fa-solid fa-circle-xmark"></i>
            </div>
            <h1 class="result-title">Confirmation Failed</h1>
            <p class="result-desc">{{ errorMsg() }}</p>
            <div class="result-actions">
              <a routerLink="/dashboard/payments" class="btn-outline-link">
                <i class="fa-solid fa-arrow-left"></i> Back to Payments
              </a>
            </div>
          }

        }

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
    }
    .result-icon.success { color: #16a34a; }
    .result-icon.error   { color: #dc2626; }
    .result-icon.loading { color: #6366f1; }
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
    .session-ref {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 8px;
      font-size: 12px;
      color: #94a3b8;
      margin-bottom: 24px;
      span { font-weight: 600; }
      code { font-size: 11px; word-break: break-all; }
    }
    .result-actions {
      display: flex;
      justify-content: center;
      gap: 12px;
      flex-wrap: wrap;
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
    .btn-outline-link {
      display: inline-flex;
      align-items: center;
      gap: 8px;
      background: transparent;
      color: #4f46e5;
      border: 1.5px solid #c7d2fe;
      border-radius: 10px;
      padding: 11px 24px;
      font-size: 14px;
      font-weight: 700;
      text-decoration: none;
      transition: background .15s;
      &:hover { background: #eef2ff; }
    }
  `],
})
export class RentPaymentSuccessComponent implements OnInit {
  private route          = inject(ActivatedRoute);
  private paymentService = inject(PaymentService);

  state    = signal<ConfirmState>('loading');
  sessionId = signal<string | null>(null);
  errorMsg  = signal<string>('We could not confirm your payment. Please check your payment history or contact support.');

  ngOnInit(): void {
    const sid = this.route.snapshot.queryParamMap.get('session_id');
    this.sessionId.set(sid);

    if (!sid) {
      this.errorMsg.set('No session ID found in the URL. Please return to your payment history.');
      this.state.set('error');
      return;
    }

    this.paymentService.confirmRentPayment(sid).subscribe({
      next: () => this.state.set('success'),
      error: (err) => {
        const msg = err?.error?.error ?? err?.error?.message;
        if (msg) this.errorMsg.set(msg);
        this.state.set('error');
      },
    });
  }
}
