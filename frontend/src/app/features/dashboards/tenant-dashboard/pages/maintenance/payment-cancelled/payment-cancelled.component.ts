import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MaintenanceService } from '../../../services/maintenance.service';

@Component({
  selector: 'app-maintenance-payment-cancelled',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="result-page">
      <div class="result-card">
        <div class="result-icon cancelled">
          <i class="fa-solid fa-circle-xmark"></i>
        </div>
        <h1 class="result-title">Payment Cancelled</h1>
        <p class="result-desc">No charge was made. You can retry the payment from your maintenance ticket at any time.</p>
        <div class="result-actions">
          <a routerLink="/dashboard/maintenance-requests" class="btn-secondary-link">
            <i class="fa-solid fa-arrow-left me-1"></i>Back to Maintenance
          </a>
          <button class="btn-retry-link" (click)="retry()">
            <i class="fa-solid fa-rotate-right me-1"></i>Retry Payment
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .result-page { display:flex; align-items:center; justify-content:center; min-height:60vh; padding:2rem; }
    .result-card { background:var(--color-surface-container-lowest); border:1px solid var(--color-outline-variant); border-radius:16px; padding:3rem 2.5rem; text-align:center; max-width:440px; width:100%; }
    .result-icon { font-size:3.5rem; margin-bottom:1.25rem; }
    .result-icon.cancelled { color:#dc2626; }
    .result-title { font-size:1.5rem; font-weight:800; margin:0 0 10px; color:var(--color-on-surface); }
    .result-desc { font-size:14px; color:var(--color-on-surface-variant); margin:0 0 24px; line-height:1.65; }
    .result-actions { display:flex; flex-direction:column; gap:10px; align-items:center; }
    .btn-secondary-link { display:inline-flex; align-items:center; gap:6px; border:1.5px solid var(--color-outline-variant); color:var(--color-on-surface); border-radius:var(--radius-md); padding:10px 22px; font-size:14px; font-weight:600; text-decoration:none; transition:all .15s; }
    .btn-secondary-link:hover { background:var(--color-surface-container); }
    .btn-retry-link { display:inline-flex; align-items:center; gap:6px; background:#2563eb; color:#fff; border:none; border-radius:var(--radius-md); padding:10px 22px; font-size:14px; font-weight:600; cursor:pointer; transition:opacity .15s; }
    .btn-retry-link:hover { opacity:.88; }
  `],
})
export class MaintenancePaymentCancelledComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly svc = inject(MaintenanceService);
  readonly retrying = signal(false);

  retry(): void {
    // Navigate back — the ticket detail will offer the checkout button again
    window.history.back();
  }
}
