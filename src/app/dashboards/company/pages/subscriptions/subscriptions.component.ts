/**
 * Company Subscription Page
 * Current plan, usage meters, active features, and (mocked) invoices.
 * Payment module is Phase 2 — upgrade shows a "coming soon" notice.
 */

import { Component, inject, signal } from '@angular/core';
import { DatePipe, CurrencyPipe } from '@angular/common';
import { toSignal } from '@angular/core/rxjs-interop';
import { CompanySubscriptionService } from '../../services/company-subscription.service';
import { UsageMeterComponent } from '../../../../shared/components/usage-meter/usage-meter.component';

interface Invoice {
  date: string;
  amount: number;
  status: string;
}

@Component({
  selector: 'app-company-subscriptions',
  standalone: true,
  imports: [DatePipe, CurrencyPipe, UsageMeterComponent],
  templateUrl: './subscriptions.component.html',
  styleUrl: './subscriptions.component.css',
})
export class SubscriptionsComponent {
  private subSvc = inject(CompanySubscriptionService);

  subscription = toSignal(this.subSvc.getActive());
  usage = toSignal(this.subSvc.getUsage());

  upgradeNotice = signal(false);

  features = [
    'Advanced analytics',
    'Featured listings',
    'AI price estimation',
    'AI lead prioritization',
  ];

  // Mocked until the Payment module ships (Phase 2)
  invoices: Invoice[] = [
    { date: '2026-06-15', amount: 99, status: 'Paid' },
    { date: '2026-05-15', amount: 99, status: 'Paid' },
    { date: '2026-04-15', amount: 99, status: 'Paid' },
  ];

  onUpgrade(): void {
    this.upgradeNotice.set(true);
    setTimeout(() => this.upgradeNotice.set(false), 4000);
  }
}
