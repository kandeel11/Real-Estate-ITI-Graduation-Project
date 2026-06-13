/**
 * Company Performance Page
 * Charts built from broker data with Bootstrap progress bars — no chart
 * library. All numbers derive from CompanyService.getBrokers() (mocked).
 */

import { Component, inject, computed } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { toSignal } from '@angular/core/rxjs-interop';
import { catchError, of } from 'rxjs';
import { CompanyService } from '../../services/company.service';
import { Broker, RatingSummary } from '../../../../shared/models/company.model';

@Component({
  selector: 'app-company-performance',
  standalone: true,
  imports: [DecimalPipe],
  templateUrl: './performance.component.html',
  styleUrl: './performance.component.css',
})
export class PerformanceComponent {
  private companySvc = inject(CompanyService);

  brokers = toSignal(this.companySvc.getBrokers(), {
    initialValue: [] as Broker[],
  });

  loading = computed(() => this.brokers().length === 0);

  private deals = (b: Broker) => b.closedDealsCount ?? 0;
  private response = (b: Broker) => b.avgResponseTimeMinutes ?? 0;

  /** True while no broker has recorded any deals yet (fresh company). */
  metricsMissing = computed(
    () =>
      this.brokers().length > 0 &&
      this.brokers().every((b) => this.deals(b) === 0)
  );

  dealsRanking = computed(() =>
    [...this.brokers()].sort((a, b) => this.deals(b) - this.deals(a))
  );

  maxDeals = computed(() =>
    Math.max(...this.brokers().map((b) => this.deals(b)), 1)
  );

  barWidth = (b: Broker) => (this.deals(b) / this.maxDeals()) * 100;

  fastest = computed(() =>
    [...this.brokers()].sort((a, b) => this.response(a) - this.response(b))[0]
  );

  slowest = computed(() =>
    [...this.brokers()].sort((a, b) => this.response(b) - this.response(a))[0]
  );

  avgResponse = computed(() => {
    const list = this.brokers();
    if (!list.length) return 0;
    return list.reduce((sum, b) => sum + this.response(b), 0) / list.length;
  });

  // Real distribution from GET /companies/mine/ratings-summary
  ratingsSummary = toSignal(
    this.companySvc.getRatingsSummary().pipe(
      catchError(() => of(null as RatingSummary | null))
    )
  );
}
