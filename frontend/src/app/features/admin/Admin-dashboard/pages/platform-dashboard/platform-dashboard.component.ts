import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PaymentService, PlatformDashboard } from '../../../../../core/services/payment.service';

@Component({
  selector: 'app-admin-platform-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './platform-dashboard.component.html',
  styleUrls: ['./platform-dashboard.component.scss'],
})
export class AdminPlatformDashboardComponent implements OnInit {
  private paymentService = inject(PaymentService);

  loading = signal(true);
  error = signal<string | null>(null);
  data = signal<PlatformDashboard | null>(null);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.paymentService.getPlatformDashboard().subscribe({
      next: (d) => { this.data.set(d); this.loading.set(false); },
      error: () => { this.error.set('Failed to load platform dashboard.'); this.loading.set(false); },
    });
  }

  fmt(n: number | null | undefined): string {
    const v = n ?? 0;
    return v.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' EGP';
  }

  pct(part: number, total: number): number {
    if (!total) return 0;
    return Math.round((part / total) * 100);
  }
}
