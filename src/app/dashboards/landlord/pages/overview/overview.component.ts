import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-landlord-overview',
  standalone: true,
  imports: [CommonModule, RouterLink],
  styles: [`
    @import url('https://fonts.googleapis.com/css2?family=Manrope:wght@400;500;600;700;800&display=swap');

    * { box-sizing: border-box; margin: 0; padding: 0; }

    .page {
      font-family: 'Manrope', sans-serif;
      background: #F8F9FA;
      min-height: 100vh;
      padding: 32px;
    }

    /* ── HEADER ── */
    .page-header {
      display: flex; align-items: flex-start;
      justify-content: space-between; margin-bottom: 32px;
    }
    .page-header h1 {
      font-size: 26px; font-weight: 800;
      color: #1E293B; letter-spacing: -0.6px;
    }
    .page-header p { font-size: 14px; color: #64748B; margin-top: 4px; font-weight: 500; }
    .header-actions { display: flex; gap: 10px; align-items: center; }

    .btn-outline {
      padding: 9px 18px;
      border: 1.5px solid #E2E8F0;
      border-radius: 10px; font-size: 13px; font-weight: 600;
      color: #1E293B; background: white; cursor: pointer;
      font-family: 'Manrope', sans-serif;
      transition: all 0.2s;
    }
    .btn-outline:hover { border-color: #4F46E5; color: #4F46E5; }

    .btn-primary {
      padding: 9px 18px;
      background: #4F46E5; border: none;
      border-radius: 10px; font-size: 13px; font-weight: 600;
      color: white; cursor: pointer;
      font-family: 'Manrope', sans-serif;
      display: flex; align-items: center; gap: 6px;
      transition: all 0.2s;
    }
    .btn-primary:hover { background: #4338CA; }
    .live-dot {
      width: 7px; height: 7px; border-radius: 50%;
      background: #EF4444;
      animation: pulse 1.5s infinite;
    }
    @keyframes pulse {
      0%, 100% { opacity: 1; }
      50% { opacity: 0.4; }
    }

    /* ── STAT CARDS ── */
    .stats-grid {
      display: grid; grid-template-columns: repeat(4, 1fr);
      gap: 16px; margin-bottom: 20px;
    }
    .stat-card {
      background: white; border-radius: 16px; padding: 22px;
      border: 1px solid #E9EDF2;
      box-shadow: 0 1px 4px rgba(0,0,0,0.04);
      transition: box-shadow 0.2s;
    }
    .stat-card:hover { box-shadow: 0 4px 16px rgba(79,70,229,0.08); }
    .stat-top {
      display: flex; align-items: center; justify-content: space-between;
      margin-bottom: 14px;
    }
    .stat-icon {
      width: 42px; height: 42px; border-radius: 12px;
      display: flex; align-items: center; justify-content: center;
      font-size: 19px;
    }
    .stat-badge { font-size: 11px; font-weight: 700; letter-spacing: 0.01em; }
    .stat-label {
      font-size: 11px; font-weight: 700; color: #94A3B8;
      text-transform: uppercase; letter-spacing: 0.07em; margin-bottom: 6px;
    }
    .stat-value {
      font-size: 34px; font-weight: 800;
      color: #1E293B; letter-spacing: -1.5px;
    }

    /* ── SUBSCRIPTION BANNER ── */
    .sub-banner {
      background: #1E293B; border-radius: 16px; padding: 20px 26px;
      display: flex; align-items: center; gap: 20px;
      margin-bottom: 24px;
    }
    .sub-badge {
      background: linear-gradient(135deg, #4F46E5, #7C3AED);
      color: white; padding: 5px 14px; border-radius: 20px;
      font-size: 12px; font-weight: 700; white-space: nowrap;
      display: flex; align-items: center; gap: 5px;
    }
    .sub-divider { width: 1px; height: 32px; background: rgba(255,255,255,0.08); flex-shrink: 0; }
    .sub-text { color: #94A3B8; font-size: 13px; white-space: nowrap; font-weight: 500; }
    .sub-meters { display: flex; gap: 28px; flex: 1; }
    .sub-meter { flex: 1; }
    .meter-label { display: flex; justify-content: space-between; margin-bottom: 7px; }
    .meter-label span { font-size: 12px; color: #94A3B8; font-weight: 500; }
    .meter-label strong { font-size: 12px; color: white; font-weight: 700; }
    .meter-track {
      height: 5px; background: rgba(255,255,255,0.08);
      border-radius: 3px; overflow: hidden;
    }
    .meter-fill { height: 5px; border-radius: 3px; transition: width 0.4s ease; }
    .btn-manage {
      padding: 10px 22px; background: white; border: none;
      border-radius: 10px; font-size: 13px; font-weight: 700;
      color: #1E293B; cursor: pointer; white-space: nowrap;
      font-family: 'Manrope', sans-serif;
      transition: all 0.2s;
    }
    .btn-manage:hover { background: #F1F5F9; }

    /* ── BOTTOM GRID ── */
    .bottom-grid {
      display: grid; grid-template-columns: 1fr 320px; gap: 20px;
    }

    /* ── CARD ── */
    .card {
      background: white; border-radius: 16px; padding: 24px;
      border: 1px solid #E9EDF2;
      box-shadow: 0 1px 4px rgba(0,0,0,0.04);
    }
    .card-header {
      display: flex; align-items: center; justify-content: space-between;
      margin-bottom: 20px;
    }
    .card-header h3 { font-size: 15px; font-weight: 800; color: #1E293B; }
    .view-all {
      font-size: 13px; color: #4F46E5;
      text-decoration: none; font-weight: 600;
      transition: color 0.2s;
    }
    .view-all:hover { color: #4338CA; }

    /* ── ACTIVITY ── */
    .activity-item {
      display: flex; align-items: flex-start; gap: 14px;
      padding: 14px 0;
      border-bottom: 1px solid #F1F5F9;
    }
    .activity-item:last-child { border-bottom: none; padding-bottom: 0; }
    .activity-icon {
      width: 38px; height: 38px; border-radius: 11px;
      background: #F1F5F9;
      display: flex; align-items: center; justify-content: center;
      font-size: 16px; flex-shrink: 0;
    }
    .activity-body { flex: 1; min-width: 0; }
    .activity-title {
      font-size: 13px; font-weight: 700;
      color: #1E293B; margin-bottom: 3px;
    }
    .activity-desc { font-size: 12px; color: #64748B; line-height: 1.55; }
    .activity-desc strong { color: #1E293B; font-weight: 700; }
    .activity-time { font-size: 11px; color: #94A3B8; white-space: nowrap; font-weight: 600; padding-top: 2px; }

    /* ── QUICK ACTIONS ── */
    .quick-actions { display: flex; flex-direction: column; gap: 10px; }
    .action-btn {
      display: flex; align-items: center; gap: 14px;
      padding: 13px 15px; background: #F8F9FA;
      border: 1px solid #E9EDF2; border-radius: 12px;
      text-decoration: none; transition: all 0.2s; cursor: pointer;
    }
    .action-btn:hover {
      background: #EEF2FF; border-color: #C7D2FE;
      transform: translateX(2px);
    }
    .action-icon {
      width: 38px; height: 38px; border-radius: 10px;
      background: #EEF2FF;
      display: flex; align-items: center; justify-content: center;
      font-size: 17px; flex-shrink: 0;
    }
    .action-label { font-size: 13px; font-weight: 700; color: #1E293B; flex: 1; }
    .action-arrow { color: #94A3B8; font-size: 18px; font-weight: 300; }

    .system-status {
      margin-top: 4px; padding: 13px 15px;
      background: #F0FDF4; border: 1px solid #BBF7D0;
      border-radius: 12px;
    }
    .status-header { display: flex; align-items: center; gap: 6px; margin-bottom: 3px; }
    .status-dot { width: 7px; height: 7px; border-radius: 50%; background: #22C55E; }
    .status-title { font-size: 11px; font-weight: 800; color: #166534; text-transform: uppercase; letter-spacing: 0.06em; }
    .status-desc { font-size: 12px; color: #4ADE80; font-weight: 500; }
    .status-desc span { color: #16A34A; }

    @media (max-width: 1024px) {
      .page { padding: 20px; }
      .stats-grid { grid-template-columns: repeat(2, 1fr); }
      .bottom-grid { grid-template-columns: 1fr; }
      .sub-meters { flex-direction: column; gap: 12px; }
      .sub-banner { flex-wrap: wrap; }
    }
  `],
  template: `
    <div class="page">

      <!-- HEADER -->
      <div class="page-header">
        <div>
          <h1>Welcome back, John 👋</h1>
          <p>Here's what's happening with your portfolio today.</p>
        </div>
        <div class="header-actions">
          <button class="btn-outline">Download Report</button>
          <button class="btn-primary">
            <span class="live-dot"></span> Real-time Feed
          </button>
        </div>
      </div>

      <!-- STAT CARDS -->
      <div class="stats-grid">
        <div class="stat-card">
          <div class="stat-top">
            <div class="stat-icon" style="background:#EEF2FF">🏢</div>
            <span class="stat-badge" style="color:#4F46E5">+2 this month</span>
          </div>
          <div class="stat-label">Total Listings</div>
          <div class="stat-value">12</div>
        </div>
        <div class="stat-card">
          <div class="stat-top">
            <div class="stat-icon" style="background:#F0FDF4">✅</div>
            <span class="stat-badge" style="color:#16A34A">67% occupancy</span>
          </div>
          <div class="stat-label">Active Units</div>
          <div class="stat-value">8</div>
        </div>
        <div class="stat-card">
          <div class="stat-top">
            <div class="stat-icon" style="background:#FFF7ED">👁️</div>
            <span class="stat-badge" style="color:#EA580C">Up 12%</span>
          </div>
          <div class="stat-label">Total Views</div>
          <div class="stat-value">245</div>
        </div>
        <div class="stat-card">
          <div class="stat-top">
            <div class="stat-icon" style="background:#FEF2F2">💬</div>
            <span class="stat-badge" style="color:#DC2626">4 urgent</span>
          </div>
          <div class="stat-label">New Inquiries</div>
          <div class="stat-value">17</div>
        </div>
      </div>

      <!-- SUBSCRIPTION BANNER -->
      <div class="sub-banner">
        <span class="sub-badge">⭐ Premium Individual</span>
        <div class="sub-divider"></div>
        <span class="sub-text">Your current usage for this billing cycle.</span>
        <div class="sub-meters">
          <div class="sub-meter">
            <div class="meter-label">
              <span>Properties (12 of 25)</span>
              <strong>48%</strong>
            </div>
            <div class="meter-track">
              <div class="meter-fill" style="width:48%; background:#4F46E5"></div>
            </div>
          </div>
          <div class="sub-meter">
            <div class="meter-label">
              <span>AI Requests (142 of 500)</span>
              <strong>28%</strong>
            </div>
            <div class="meter-track">
              <div class="meter-fill" style="width:28%; background:#F59E0B"></div>
            </div>
          </div>
        </div>
        <button class="btn-manage">Manage Plan</button>
      </div>

      <!-- BOTTOM GRID -->
      <div class="bottom-grid">

        <!-- RECENT ACTIVITY -->
        <div class="card">
          <div class="card-header">
            <h3>Recent Activity</h3>
            <a href="#" class="view-all">View all</a>
          </div>
          @for (item of activities(); track item.id) {
            <div class="activity-item">
              <div class="activity-icon">{{ item.icon }}</div>
              <div class="activity-body">
                <div class="activity-title">{{ item.title }}</div>
                <div class="activity-desc" [innerHTML]="item.desc"></div>
              </div>
              <div class="activity-time">{{ item.time }}</div>
            </div>
          }
        </div>

        <!-- QUICK ACTIONS -->
        <div class="card">
          <div class="card-header">
            <h3>Quick Actions</h3>
          </div>
          <div class="quick-actions">
            <a routerLink="../properties" class="action-btn">
              <div class="action-icon">🏢</div>
              <span class="action-label">Add New Property</span>
              <span class="action-arrow">›</span>
            </a>
            <a routerLink="../analytics" class="action-btn">
              <div class="action-icon">📈</div>
              <span class="action-label">View Analytics</span>
              <span class="action-arrow">›</span>
            </a>
            <a href="#" class="action-btn">
              <div class="action-icon">🤖</div>
              <span class="action-label">Run AI Check</span>
              <span class="action-arrow">›</span>
            </a>
            <div class="system-status">
              <div class="status-header">
                <div class="status-dot"></div>
                <div class="status-title">System Status</div>
              </div>
              <div class="status-desc">
                <span>All real estate nodes are active.</span> Next portfolio sync in 12m.
              </div>
            </div>
          </div>
        </div>

      </div>
    </div>
  `,
})
export class LandlordOverviewComponent {
  activities = signal([
    {
      id: 1,
      icon: '👥',
      title: 'New Tenant Application',
      desc: 'Sarah Miller applied for the <strong>Skyline Apartments - Unit 402</strong>.',
      time: '2 hours ago'
    },
    {
      id: 2,
      icon: '📄',
      title: 'Lease Renewed',
      desc: 'Digital contract signed for <strong>Oak Ridge Estate</strong>. Term: 12 months.',
      time: '5 hours ago'
    },
    {
      id: 3,
      icon: '🔧',
      title: 'Maintenance Request',
      desc: 'Urgent: HVAC repair needed at <strong>Harbor View Condos</strong>.',
      time: 'Yesterday'
    },
  ]);
}