import {
  Component,
  OnInit,
  inject,
  DestroyRef,
  AfterViewInit,
  ElementRef,
  ViewChild,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { BrokerDataService } from '../../services/broker-data.service';
import { PerformanceStats, Rating } from '../../models/broker.models';

@Component({
  selector: 'app-broker-performance',
  standalone: true,
  imports: [CommonModule],
  styles: [
    `
      .page {
        padding: 2rem;
        max-width: 1100px;
        margin: 0 auto;
      }
      .page-header {
        margin-bottom: 1.5rem;
      }
      .page-header h1 {
        font-size: 1.4rem;
        font-weight: 600;
        color: #1a1a2e;
        margin: 0;
      }
      .kpi-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
        gap: 1rem;
        margin-bottom: 2rem;
      }
      .kpi-card {
        background: #fff;
        border-radius: 12px;
        padding: 1.25rem;
        box-shadow: 0 1px 4px rgba(0, 0, 0, 0.08);
        text-align: center;
      }
      .kpi-value {
        font-size: 2rem;
        font-weight: 700;
        color: #2d6a4f;
        margin-bottom: 0.25rem;
      }
      .kpi-label {
        font-size: 0.85rem;
        color: #6c757d;
      }
      .ranking-card {
        background: linear-gradient(135deg, #2d6a4f, #1b4332);
        color: #fff;
        border-radius: 12px;
        padding: 1.25rem;
        text-align: center;
      }
      .ranking-card .kpi-value {
        color: #fff;
      }
      .ranking-card .kpi-label {
        color: #a7f3d0;
      }
      .bottom-grid {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 1.5rem;
      }
      @media (max-width: 768px) {
        .bottom-grid {
          grid-template-columns: 1fr;
        }
      }
      .card {
        background: #fff;
        border-radius: 12px;
        padding: 1.25rem;
        box-shadow: 0 1px 4px rgba(0, 0, 0, 0.08);
      }
      .card h3 {
        font-size: 1rem;
        font-weight: 600;
        color: #1a1a2e;
        margin: 0 0 1rem;
      }
      .chart-wrap {
        position: relative;
        height: 200px;
        display: flex;
        align-items: flex-end;
        gap: 8px;
        padding: 0 0.5rem;
      }
      .bar-group {
        flex: 1;
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 4px;
        height: 100%;
        justify-content: flex-end;
      }
      .bar {
        width: 100%;
        border-radius: 6px 6px 0 0;
        background: #2d6a4f;
        transition: height 0.5s ease;
        min-height: 4px;
      }
      .bar:hover {
        background: #1b4332;
      }
      .bar-label {
        font-size: 0.72rem;
        color: #888;
      }
      .bar-val {
        font-size: 0.72rem;
        color: #2d6a4f;
        font-weight: 600;
      }
      .rating-item {
        display: flex;
        gap: 0.75rem;
        align-items: flex-start;
        padding: 0.75rem 0;
        border-bottom: 1px solid #f0f0f0;
      }
      .rating-item:last-child {
        border-bottom: none;
      }
      .stars {
        font-size: 0.88rem;
        color: #f59e0b;
        white-space: nowrap;
      }
      .rating-body {
        flex: 1;
      }
      .rating-comment {
        font-size: 0.88rem;
        color: #333;
        margin: 0 0 0.2rem;
      }
      .rating-reviewer {
        font-size: 0.78rem;
        color: #aaa;
      }
      .skeleton {
        background: linear-gradient(90deg, #f0f0f0 25%, #e0e0e0 50%, #f0f0f0 75%);
        background-size: 200% 100%;
        animation: shimmer 1.5s infinite;
        border-radius: 8px;
      }
      @keyframes shimmer {
        0% {
          background-position: 200% 0;
        }
        100% {
          background-position: -200% 0;
        }
      }
    `,
  ],
  template: `
    <div class="page">
      <div class="page-header">
        <h1>My Performance</h1>
      </div>

      <!-- KPI Cards -->
      @if (loading) {
        <div class="kpi-grid">
          @for (i of [1, 2, 3, 4]; track i) {
            <div class="kpi-card">
              <div class="skeleton" style="height:48px; margin-bottom:8px;"></div>
              <div class="skeleton" style="height:14px; width:70%;"></div>
            </div>
          }
        </div>
      } @else if (stats) {
        <div class="kpi-grid">
          <div class="kpi-card">
            <div class="kpi-value">{{ stats.dealsClosedThisMonth }}</div>
            <div class="kpi-label">Deals This Month</div>
          </div>
          <div class="kpi-card">
            <div class="kpi-value">{{ stats.avgResponseTimeMinutes }}m</div>
            <div class="kpi-label">Avg Response Time</div>
          </div>
          <div class="kpi-card">
            <div class="kpi-value">{{ stats.conversionRate }}%</div>
            <div class="kpi-label">Conversion Rate</div>
          </div>
          <div class="kpi-card">
            <div class="kpi-value">⭐ {{ stats.rating }}</div>
            <div class="kpi-label">Rating ({{ stats.ratingCount }} reviews)</div>
          </div>
          <div class="kpi-card ranking-card">
            <div class="kpi-value">#{{ stats.rankInCompany }}</div>
            <div class="kpi-label">Company Ranking out of {{ stats.totalBrokers }}</div>
          </div>
        </div>
      }

      <!-- Chart + Ratings -->
      <div class="bottom-grid">
        <!-- Bar Chart -->
        <div class="card">
          <h3>Deals Closed — Last 6 Months</h3>
          @if (loading) {
            <div class="skeleton" style="height:200px;"></div>
          } @else if (stats) {
            <div class="chart-wrap">
              @for (item of stats.monthlyDeals; track item.month) {
                <div class="bar-group">
                  <span class="bar-val">{{ item.deals }}</span>
                  <div class="bar" [style.height.%]="barHeight(item.deals)"></div>
                  <span class="bar-label">{{ item.month }}</span>
                </div>
              }
            </div>
          }
        </div>

        <!-- Ratings List -->
        <div class="card">
          <h3>Recent Ratings</h3>
          @if (loading) {
            @for (i of [1, 2, 3]; track i) {
              <div class="skeleton" style="height:60px; margin-bottom:8px;"></div>
            }
          } @else if (ratings.length === 0) {
            <p style="color:#aaa; font-size:0.9rem;">No ratings yet.</p>
          } @else {
            @for (r of ratings; track r.id) {
              <div class="rating-item">
                <div class="stars">{{ starsDisplay(r.stars) }}</div>
                <div class="rating-body">
                  <p class="rating-comment">{{ r.comment }}</p>
                  <span class="rating-reviewer">— {{ r.reviewer }}</span>
                </div>
              </div>
            }
          }
        </div>
      </div>
    </div>
  `,
})
export class PerformanceComponent implements OnInit {
  private brokerService = inject(BrokerDataService);
  private destroyRef = inject(DestroyRef);

  stats: PerformanceStats | null = null;
  ratings: Rating[] = [];
  loading = true;

  ngOnInit() {
    this.brokerService
      .getPerformance()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((s) => {
        this.stats = s;
        this.checkDone();
      });

    this.brokerService
      .getRatings()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((r) => {
        this.ratings = r;
        this.checkDone();
      });
  }

  private checkDone() {
    if (this.stats && this.ratings.length >= 0) this.loading = false;
  }

  barHeight(deals: number): number {
    const max = Math.max(...(this.stats?.monthlyDeals.map((d) => d.deals) ?? [1]));
    return Math.round((deals / max) * 85);
  }

  starsDisplay(count: number): string {
    return '★'.repeat(count) + '☆'.repeat(5 - count);
  }
}
