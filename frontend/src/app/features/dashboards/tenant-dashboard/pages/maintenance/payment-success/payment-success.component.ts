import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MaintenanceService } from '../../../services/maintenance.service';

@Component({
  selector: 'app-maintenance-payment-success',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="result-page">
      <div class="result-card">
        <div class="result-icon success">
          <i class="fa-solid fa-circle-check"></i>
        </div>
        <h1 class="result-title">Payment Successful</h1>
        <p class="result-desc">Your maintenance payment has been confirmed. The ticket status will update shortly.</p>
        @if (sessionId()) {
          <p class="result-session">Session: <code>{{ sessionId() }}</code></p>
        }
        <div class="result-actions">
          <a routerLink="/dashboard/maintenance-requests" class="btn-primary-link">
            <i class="fa-solid fa-arrow-left me-1"></i>Back to Maintenance
          </a>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .result-page { display:flex; align-items:center; justify-content:center; min-height:60vh; padding:2rem; }
    .result-card { background:var(--color-surface-container-lowest); border:1px solid var(--color-outline-variant); border-radius:16px; padding:3rem 2.5rem; text-align:center; max-width:440px; width:100%; }
    .result-icon { font-size:3.5rem; margin-bottom:1.25rem; }
    .result-icon.success { color:#16a34a; }
    .result-title { font-size:1.5rem; font-weight:800; margin:0 0 10px; color:var(--color-on-surface); }
    .result-desc { font-size:14px; color:var(--color-on-surface-variant); margin:0 0 20px; line-height:1.65; }
    .result-session { font-size:11px; color:var(--color-on-surface-variant); margin-bottom:20px; }
    .result-session code { font-size:11px; }
    .btn-primary-link { display:inline-flex; align-items:center; gap:6px; background:var(--color-primary); color:#fff; border-radius:var(--radius-md); padding:10px 22px; font-size:14px; font-weight:600; text-decoration:none; transition:opacity .15s; }
    .btn-primary-link:hover { opacity:.88; }
  `],
})
export class MaintenancePaymentSuccessComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  readonly sessionId = signal<string | null>(null);

  ngOnInit(): void {
    this.sessionId.set(this.route.snapshot.queryParamMap.get('session_id'));
  }
}
