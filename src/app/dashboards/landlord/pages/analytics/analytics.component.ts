import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-landlord-analytics',
  standalone: true,
  imports: [CommonModule],
  styles: [`
    * { box-sizing: border-box; margin: 0; padding: 0; }
    .page { font-family: 'Inter', 'Segoe UI', sans-serif; }

    .page-header {
      display: flex; align-items: center; justify-content: space-between;
      margin-bottom: 28px;
    }
    .page-header h1 { font-size: 28px; font-weight: 800; color: #0f172a; letter-spacing: -0.5px; }
    .page-header p { font-size: 14px; color: #64748b; margin-top: 4px; }
    .date-filters { display: flex; gap: 6px; }
    .date-btn {
      padding: 8px 16px; border-radius: 8px; font-size: 13px; font-weight: 600;
      border: 1.5px solid #e2e8f0; background: white; color: #374151;
      cursor: pointer; transition: all 0.2s;
    }
    .date-btn.active { background: #2563eb; border-color: #2563eb; color: white; }
    .date-btn:hover:not(.active) { border-color: #2563eb; color: #2563eb; }

    /* TOP ROW */
    .top-row { display: grid; grid-template-columns: 1fr 340px; gap: 20px; margin-bottom: 20px; }

    /* CHART CARD */
    .card {
      background: white; border-radius: 16px; padding: 24px;
      border: 1px solid #f1f5f9;
      box-shadow: 0 2px 8px rgba(0,0,0,0.04);
    }
    .card-header { display: flex; align-items: flex-start; justify-content: space-between; margin-bottom: 8px; }
    .card-header h3 { font-size: 16px; font-weight: 700; color: #0f172a; }
    .card-actions { display: flex; gap: 8px; }
    .icon-btn {
      width: 32px; height: 32px; border-radius: 8px;
      border: 1.5px solid #e2e8f0; background: white;
      display: flex; align-items: center; justify-content: center;
      cursor: pointer; font-size: 13px;
    }
    .chart-value { font-size: 28px; font-weight: 800; color: #2563eb; letter-spacing: -1px; }
    .chart-badge {
      display: inline-flex; align-items: center; gap: 4px;
      background: #f0fdf4; color: #16a34a;
      padding: 3px 8px; border-radius: 6px;
      font-size: 12px; font-weight: 600; margin-left: 8px;
    }

    /* SVG CHART */
    .chart-wrap { margin-top: 16px; }
    svg { width: 100%; overflow: visible; }

    /* INQUIRIES BY PROPERTY */
    .inquiry-list { display: flex; flex-direction: column; gap: 16px; margin-top: 8px; }
    .inquiry-item { }
    .inquiry-top { display: flex; justify-content: space-between; margin-bottom: 6px; }
    .inquiry-name { font-size: 12px; font-weight: 600; color: #64748b; text-transform: uppercase; letter-spacing: 0.05em; }
    .inquiry-count { font-size: 13px; font-weight: 700; color: #0f172a; }
    .inquiry-track { height: 6px; background: #f1f5f9; border-radius: 3px; }
    .inquiry-fill { height: 6px; background: #2563eb; border-radius: 3px; transition: width 0.5s; }
    .view-report {
      display: block; text-align: center; margin-top: 20px;
      padding: 10px; border: 1.5px solid #e2e8f0; border-radius: 10px;
      font-size: 13px; font-weight: 600; color: #2563eb; text-decoration: none;
      transition: all 0.2s; cursor: pointer; background: white;
    }
    .view-report:hover { background: #eff6ff; }

    /* TOP PERFORMERS TABLE */
    .table-card { margin-top: 20px; }
    .table-header { display: flex; align-items: center; justify-content: space-between; margin-bottom: 20px; }
    .table-header h3 { font-size: 16px; font-weight: 700; color: #0f172a; }
    .btn-filter {
      padding: 7px 14px; border: 1.5px solid #e2e8f0; border-radius: 8px;
      font-size: 13px; font-weight: 600; color: #374151; background: white;
      cursor: pointer; display: flex; align-items: center; gap: 6px;
    }
    table { width: 100%; border-collapse: collapse; }
    thead th {
      font-size: 11px; font-weight: 700; color: #94a3b8;
      text-transform: uppercase; letter-spacing: 0.06em;
      padding: 8px 16px; text-align: left;
      border-bottom: 1px solid #f1f5f9;
    }
    tbody tr { border-bottom: 1px solid #f8fafc; transition: background 0.2s; }
    tbody tr:hover { background: #f8fafc; }
    tbody tr:last-child { border-bottom: none; }
    td { padding: 14px 16px; }
    .rank { font-size: 18px; font-weight: 800; color: #cbd5e1; }
    .rank.top { color: #2563eb; }
    .prop-thumb {
      width: 40px; height: 40px; border-radius: 8px; object-fit: cover;
    }
    .prop-name { font-size: 14px; font-weight: 600; color: #0f172a; }
    .prop-location { font-size: 12px; color: #94a3b8; }
    .views-val { font-size: 14px; font-weight: 600; color: #0f172a; }
    .ctr-val { font-size: 14px; font-weight: 600; color: #0f172a; }
    .trend-badge {
      padding: 3px 8px; border-radius: 6px; font-size: 12px; font-weight: 700;
    }
    .trend-up { background: #f0fdf4; color: #16a34a; }
    .trend-down { background: #fef2f2; color: #dc2626; }
    .pagination {
      display: flex; align-items: center; justify-content: space-between;
      margin-top: 20px; padding-top: 16px; border-top: 1px solid #f1f5f9;
    }
    .pagination-info { font-size: 13px; color: #64748b; }
    .pagination-btns { display: flex; gap: 6px; }
    .page-btn {
      width: 32px; height: 32px; border-radius: 8px;
      border: 1.5px solid #e2e8f0; background: white;
      font-size: 13px; cursor: pointer; display: flex;
      align-items: center; justify-content: center; transition: all 0.2s;
    }
    .page-btn:hover { border-color: #2563eb; color: #2563eb; }
  `],
  template: `
    <div class="page">

      <!-- HEADER -->
      <div class="page-header">
        <div>
          <h1>Performance Analytics</h1>
          <p>Real-time engagement tracking across your property portfolio.</p>
        </div>
        <div class="date-filters">
          <button class="date-btn" [class.active]="selectedPeriod() === '7'">7 Days</button>
          <button class="date-btn" [class.active]="selectedPeriod() === '30'" (click)="selectedPeriod.set('30')">30 Days</button>
          <button class="date-btn" [class.active]="selectedPeriod() === '90'" (click)="selectedPeriod.set('90')">90 Days</button>
          <button class="date-btn">📅 Custom</button>
        </div>
      </div>

      <!-- TOP ROW -->
      <div class="top-row">

        <!-- LINE CHART -->
        <div class="card">
          <div class="card-header">
            <div>
              <h3>Total Views over 30 days</h3>
              <div style="margin-top:6px">
                <span class="chart-value">142.8k</span>
                <span class="chart-badge">↑ 12.4%</span>
              </div>
            </div>
            <div class="card-actions">
              <button class="icon-btn">↓</button>
              <button class="icon-btn">⋮</button>
            </div>
          </div>
          <div class="chart-wrap">
            <svg viewBox="0 0 600 180" xmlns="http://www.w3.org/2000/svg">
              <defs>
                <linearGradient id="grad" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stop-color="#2563eb" stop-opacity="0.15"/>
                  <stop offset="100%" stop-color="#2563eb" stop-opacity="0"/>
                </linearGradient>
              </defs>
              <!-- Grid lines -->
              <line x1="0" y1="40" x2="600" y2="40" stroke="#f1f5f9" stroke-width="1"/>
              <line x1="0" y1="80" x2="600" y2="80" stroke="#f1f5f9" stroke-width="1"/>
              <line x1="0" y1="120" x2="600" y2="120" stroke="#f1f5f9" stroke-width="1"/>
              <line x1="0" y1="160" x2="600" y2="160" stroke="#f1f5f9" stroke-width="1"/>
              <!-- Area fill -->
              <path d="M0,140 C50,130 80,120 120,100 C160,80 180,110 220,90 C260,70 300,50 340,30 C380,10 420,60 460,40 C500,20 550,50 600,60 L600,160 L0,160 Z"
                fill="url(#grad)"/>
              <!-- Line -->
              <path d="M0,140 C50,130 80,120 120,100 C160,80 180,110 220,90 C260,70 300,50 340,30 C380,10 420,60 460,40 C500,20 550,50 600,60"
                fill="none" stroke="#2563eb" stroke-width="2.5" stroke-linecap="round"/>
              <!-- X labels -->
              <text x="0" y="178" font-size="10" fill="#94a3b8">OCT 01</text>
              <text x="140" y="178" font-size="10" fill="#94a3b8">OCT 08</text>
              <text x="280" y="178" font-size="10" fill="#94a3b8">OCT 15</text>
              <text x="420" y="178" font-size="10" fill="#94a3b8">OCT 22</text>
              <text x="555" y="178" font-size="10" fill="#94a3b8">OCT 30</text>
            </svg>
          </div>
        </div>

        <!-- INQUIRIES BY PROPERTY -->
        <div class="card">
          <div class="card-header">
            <h3>Inquiries by Property</h3>
          </div>
          <div class="inquiry-list">
            @for (item of inquiries(); track item.name) {
              <div class="inquiry-item">
                <div class="inquiry-top">
                  <span class="inquiry-name">{{ item.name }}</span>
                  <span class="inquiry-count">{{ item.count }}</span>
                </div>
                <div class="inquiry-track">
                  <div class="inquiry-fill" [style.width]="item.percent + '%'"></div>
                </div>
              </div>
            }
          </div>
          <button class="view-report">View Detailed Report</button>
        </div>

      </div>

      <!-- TOP PERFORMERS TABLE -->
      <div class="card table-card">
        <div class="table-header">
          <h3>Top Performing Properties</h3>
          <button class="btn-filter">⚙ Filters</button>
        </div>
        <table>
          <thead>
            <tr>
              <th>Rank</th>
              <th>Property Name</th>
              <th>Total Views</th>
              <th>CTR</th>
              <th>Trend</th>
            </tr>
          </thead>
          <tbody>
            @for (prop of topProperties(); track prop.rank) {
              <tr>
                <td><span class="rank" [class.top]="prop.rank === 1">{{ prop.rank < 10 ? '0' + prop.rank : prop.rank }}</span></td>
                <td>
                  <div style="display:flex; align-items:center; gap:12px">
                    <img class="prop-thumb" [src]="prop.image" [alt]="prop.name" />
                    <div>
                      <div class="prop-name">{{ prop.name }}</div>
                      <div class="prop-location">{{ prop.location }}</div>
                    </div>
                  </div>
                </td>
                <td><span class="views-val">{{ prop.views | number }}</span></td>
                <td><span class="ctr-val">{{ prop.ctr }}%</span></td>
                <td>
                  <span class="trend-badge" [class.trend-up]="prop.trend > 0" [class.trend-down]="prop.trend < 0">
                    {{ prop.trend > 0 ? '↑' : '↓' }} {{ prop.trend | number }}%
                  </span>
                </td>
              </tr>
            }
          </tbody>
        </table>
        <div class="pagination">
          <span class="pagination-info">Showing 4 of 28 properties</span>
          <div class="pagination-btns">
            <button class="page-btn">‹</button>
            <button class="page-btn">›</button>
          </div>
        </div>
      </div>

    </div>
  `,
})
export class LandlordAnalyticsComponent {
  selectedPeriod = signal('30');

  inquiries = signal([
    { name: 'Skyline Tower A', count: 842, percent: 100 },
    { name: 'Oakwood Residences', count: 614, percent: 73 },
    { name: 'The Atrium Hub', count: 520, percent: 62 },
    { name: 'Harbor Plaza', count: 311, percent: 37 },
    { name: 'Sunset Terraces', count: 245, percent: 29 },
  ]);

  topProperties = signal([
    { rank: 1, name: 'Skyline Tower A', location: 'Sheikh Zayed, Cairo', views: 24502, ctr: 4.8, trend: 15, image: 'https://images.unsplash.com/photo-1600596542815-ffad4c1539a9?w=80&auto=format&fit=crop' },
    { rank: 2, name: 'Oakwood Residences', location: 'Maadi, Cairo', views: 18290, ctr: 3.2, trend: 8, image: 'https://images.unsplash.com/photo-1613490493576-7fde63acd811?w=80&auto=format&fit=crop' },
    { rank: 3, name: 'The Atrium Hub', location: 'New Cairo, Cairo', views: 15704, ctr: 2.9, trend: -2, image: 'https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=80&auto=format&fit=crop' },
    { rank: 4, name: 'Harbor Plaza', location: 'North Coast, Egypt', views: 12110, ctr: 2.1, trend: 4, image: 'https://images.unsplash.com/photo-1512917774080-9991f1c4c750?w=80&auto=format&fit=crop' },
  ]);
}