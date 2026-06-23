import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-landlord-subscription',
  standalone: true,
  imports: [CommonModule],
  styles: [`
    * { box-sizing: border-box; margin: 0; padding: 0; }
    .page { font-family: 'Inter', 'Segoe UI', sans-serif; }

    .page-header { margin-bottom: 28px; }
    .page-header h1 { font-size: 28px; font-weight: 800; color: #0f172a; letter-spacing: -0.5px; }
    .page-header p { font-size: 14px; color: #64748b; margin-top: 4px; }

    .top-row { display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 16px; margin-bottom: 20px; }

    /* PLAN CARD */
    .plan-card {
      background: white; border-radius: 16px; padding: 24px;
      border: 1px solid #f1f5f9;
      box-shadow: 0 2px 8px rgba(0,0,0,0.04);
      grid-column: span 1;
    }
    .plan-label { font-size: 11px; font-weight: 700; color: #2563eb; text-transform: uppercase; letter-spacing: 0.08em; margin-bottom: 8px; }
    .plan-name { font-size: 22px; font-weight: 800; color: #0f172a; margin-bottom: 4px; }
    .plan-price { font-size: 28px; font-weight: 800; color: #2563eb; }
    .plan-price span { font-size: 14px; font-weight: 500; color: #64748b; }
    .plan-renewal { font-size: 13px; color: #64748b; margin-bottom: 16px; }
    .plan-features { display: flex; flex-direction: column; gap: 8px; margin-bottom: 20px; }
    .plan-feature { font-size: 13px; color: #374151; display: flex; align-items: center; gap: 8px; }
    .plan-feature::before { content: '✓'; color: #16a34a; font-weight: 700; }
    .plan-actions { display: flex; gap: 10px; }
    .btn-manage {
      flex: 1; padding: 10px; background: #2563eb; border: none;
      border-radius: 10px; font-size: 13px; font-weight: 600;
      color: white; cursor: pointer; transition: all 0.2s;
    }
    .btn-manage:hover { background: #1d4ed8; }
    .btn-cancel {
      padding: 10px 16px; background: white;
      border: 1.5px solid #e2e8f0; border-radius: 10px;
      font-size: 13px; font-weight: 600; color: #374151;
      cursor: pointer; transition: all 0.2s;
    }
    .btn-cancel:hover { border-color: #ef4444; color: #ef4444; }

    /* USAGE CARDS */
    .usage-card {
      background: white; border-radius: 16px; padding: 24px;
      border: 1px solid #f1f5f9;
      box-shadow: 0 2px 8px rgba(0,0,0,0.04);
    }
    .usage-top { display: flex; align-items: flex-start; justify-content: space-between; margin-bottom: 8px; }
    .usage-label { font-size: 13px; font-weight: 700; color: #374151; text-transform: uppercase; letter-spacing: 0.05em; }
    .usage-icon { font-size: 20px; }
    .usage-value { font-size: 28px; font-weight: 800; color: #0f172a; margin-bottom: 4px; }
    .usage-max { font-size: 13px; color: #94a3b8; margin-bottom: 12px; }
    .usage-track { height: 8px; background: #f1f5f9; border-radius: 4px; margin-bottom: 8px; }
    .usage-fill { height: 8px; border-radius: 4px; transition: width 0.5s; }
    .usage-status { font-size: 12px; font-weight: 600; }
    .status-optimal { color: #16a34a; }
    .status-good { color: #f59e0b; }
    .status-danger { color: #ef4444; }

    /* INVOICES */
    .invoices-card {
      background: white; border-radius: 16px; padding: 24px;
      border: 1px solid #f1f5f9;
      box-shadow: 0 2px 8px rgba(0,0,0,0.04);
      margin-bottom: 20px;
    }
    .invoices-header { display: flex; align-items: center; justify-content: space-between; margin-bottom: 6px; }
    .invoices-header h3 { font-size: 16px; font-weight: 700; color: #0f172a; }
    .invoices-sub { font-size: 13px; color: #64748b; margin-bottom: 20px; }
    .btn-filter {
      padding: 7px 14px; border: 1.5px solid #e2e8f0; border-radius: 8px;
      font-size: 13px; font-weight: 600; color: #374151; background: white;
      cursor: pointer;
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
    td { padding: 14px 16px; font-size: 14px; color: #374151; }
    .paid-badge {
      background: #f0fdf4; color: #16a34a;
      padding: 3px 10px; border-radius: 6px;
      font-size: 12px; font-weight: 700;
    }
    .download-btn {
      background: none; border: none; cursor: pointer;
      font-size: 18px; color: #2563eb; transition: transform 0.2s;
    }
    .download-btn:hover { transform: translateY(2px); }
    .table-footer { display: flex; align-items: center; justify-content: space-between; margin-top: 16px; padding-top: 16px; border-top: 1px solid #f1f5f9; }
    .table-footer span { font-size: 13px; color: #64748b; }
    .pagination-btns { display: flex; gap: 6px; }
    .page-btn {
      width: 32px; height: 32px; border-radius: 8px;
      border: 1.5px solid #e2e8f0; background: white;
      font-size: 13px; cursor: pointer; display: flex;
      align-items: center; justify-content: center;
    }
    .page-btn:hover { border-color: #2563eb; color: #2563eb; }

    /* UPGRADE BANNER */
    .upgrade-banner {
      background: linear-gradient(135deg, #2563eb 0%, #1e40af 100%);
      border-radius: 20px; padding: 40px 48px;
      display: flex; align-items: center; justify-content: space-between;
      position: relative; overflow: hidden;
    }
    .upgrade-banner::before {
      content: ''; position: absolute; top: -40px; right: -40px;
      width: 200px; height: 200px; border-radius: 50%;
      background: rgba(255,255,255,0.08);
    }
    .upgrade-text h2 { font-size: 24px; font-weight: 800; color: white; margin-bottom: 8px; }
    .upgrade-text p { font-size: 14px; color: rgba(255,255,255,0.7); max-width: 480px; line-height: 1.6; }
    .btn-upgrade {
      padding: 14px 28px; background: white; border: none;
      border-radius: 12px; font-size: 14px; font-weight: 700;
      color: #2563eb; cursor: pointer; transition: all 0.2s;
      white-space: nowrap; position: relative; z-index: 1;
    }
    .btn-upgrade:hover { transform: translateY(-2px); box-shadow: 0 8px 24px rgba(0,0,0,0.15); }
  `],
  template: `
    <div class="page">

      <div class="page-header">
        <h1>Subscription & Billing</h1>
        <p>Manage your agency plan, track usage, and view past invoices.</p>
      </div>

      <!-- TOP ROW -->
      <div class="top-row">

        <!-- PLAN CARD -->
        <div class="plan-card">
          <div class="plan-label">Current Plan</div>
          <div class="plan-name">Premium Individual</div>
          <div class="plan-price">$29<span>/mo</span></div>
          <div class="plan-renewal">Next bill: Oct 12, 2025</div>
          <div class="plan-features">
            <div class="plan-feature">Unlimited priority support</div>
            <div class="plan-feature">Advanced analytics dashboards</div>
            <div class="plan-feature">API Access for external integrations</div>
          </div>
          <div class="plan-actions">
            <button class="btn-manage">Manage Plan</button>
            <button class="btn-cancel">Cancel</button>
          </div>
        </div>

        <!-- PROPERTIES USAGE -->
        <div class="usage-card">
          <div class="usage-top">
            <span class="usage-label">Properties</span>
            <span class="usage-icon">🏢</span>
          </div>
          <div class="usage-value">12 / 25</div>
          <div class="usage-max">48% Capacity</div>
          <div class="usage-track">
            <div class="usage-fill" style="width:48%; background:#2563eb"></div>
          </div>
          <span class="usage-status status-optimal">● OPTIMAL</span>
        </div>

        <!-- AI REQUESTS USAGE -->
        <div class="usage-card">
          <div class="usage-top">
            <span class="usage-label">AI Requests</span>
            <span class="usage-icon">🤖</span>
          </div>
          <div class="usage-value">142 / 500</div>
          <div class="usage-max">28% Capacity</div>
          <div class="usage-track">
            <div class="usage-fill" style="width:28%; background:#f59e0b"></div>
          </div>
          <span class="usage-status status-good">● GOOD</span>
        </div>

      </div>

      <!-- FEATURED LISTINGS USAGE -->
      <div class="top-row" style="grid-template-columns: 1fr; margin-bottom: 20px;">
        <div class="usage-card">
          <div class="usage-top">
            <span class="usage-label">Featured Listings</span>
            <span class="usage-icon">⭐</span>
          </div>
          <div class="usage-value">4 / 5</div>
          <div class="usage-max">80% Capacity</div>
          <div class="usage-track">
            <div class="usage-fill" style="width:80%; background:#ef4444"></div>
          </div>
          <span class="usage-status status-danger">● LIMIT REACHED SOON</span>
        </div>
      </div>

      <!-- INVOICES -->
      <div class="invoices-card">
        <div class="invoices-header">
          <div>
            <h3>Recent Invoices</h3>
          </div>
          <button class="btn-filter">⚙ Filter</button>
        </div>
        <p class="invoices-sub">Download and view your transaction history.</p>
        <table>
          <thead>
            <tr>
              <th>Date</th>
              <th>Invoice ID</th>
              <th>Amount</th>
              <th>Status</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            @for (invoice of invoices(); track invoice.id) {
              <tr>
                <td>{{ invoice.date }}</td>
                <td>{{ invoice.id }}</td>
                <td style="font-weight:600">{{ invoice.amount }}</td>
                <td><span class="paid-badge">PAID</span></td>
                <td><button class="download-btn">↓</button></td>
              </tr>
            }
          </tbody>
        </table>
        <div class="table-footer">
          <span>Showing 3 of 12 invoices</span>
          <div class="pagination-btns">
            <button class="page-btn">‹</button>
            <button class="page-btn">›</button>
          </div>
        </div>
      </div>

      <!-- UPGRADE BANNER -->
      <div class="upgrade-banner">
        <div class="upgrade-text">
          <h2>Scale your agency to the next level.</h2>
          <p>Get the Team Enterprise plan and unlock white-labeling, multi-user permissions, and 24/7 dedicated account management.</p>
        </div>
        <button class="btn-upgrade">Upgrade to Enterprise</button>
      </div>

    </div>
  `,
})
export class LandlordSubscriptionComponent {
  invoices = signal([
    { id: '#INV-98234-P', date: 'Sept 12, 2023', amount: '$29.00' },
    { id: '#INV-97102-P', date: 'Aug 12, 2023', amount: '$29.00' },
    { id: '#INV-96055-P', date: 'July 12, 2023', amount: '$29.00' },
  ]);
}