import { Component, signal, inject } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-landlord-shell',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  styles: [`
    * { box-sizing: border-box; margin: 0; padding: 0; }

    .shell {
      display: flex; min-height: 100vh;
      font-family: 'Inter', 'Segoe UI', sans-serif;
      background: #f8fafc;
    }

    /* SIDEBAR */
    .sidebar {
      width: 260px; min-height: 100vh;
      background: #0f172a;
      display: flex; flex-direction: column;
      position: fixed; left: 0; top: 0; bottom: 0;
      z-index: 100;
    }
    .sidebar-brand {
      padding: 24px 20px 20px;
      border-bottom: 1px solid rgba(255,255,255,0.06);
    }
    .brand-title {
      color: white; font-size: 18px; font-weight: 700;
      letter-spacing: -0.5px;
    }
    .brand-sub {
      color: #64748b; font-size: 11px;
      text-transform: uppercase; letter-spacing: 0.08em;
      margin-top: 2px;
    }
    .sidebar-nav {
      flex: 1; padding: 16px 12px;
      display: flex; flex-direction: column; gap: 4px;
    }
    .nav-item {
      display: flex; align-items: center; gap: 12px;
      padding: 10px 12px; border-radius: 10px;
      color: #94a3b8; font-size: 14px; font-weight: 500;
      text-decoration: none; transition: all 0.2s;
      cursor: pointer;
    }
    .nav-item:hover { background: rgba(255,255,255,0.06); color: white; }
    .nav-item.active {
      background: rgba(37,99,235,0.2);
      color: #60a5fa;
    }
    .nav-icon { font-size: 18px; width: 20px; text-align: center; }
    .sidebar-bottom {
      padding: 16px 12px;
      border-top: 1px solid rgba(255,255,255,0.06);
    }
    .btn-new-property {
      width: 100%; padding: 12px;
      background: #2563eb; border: none; border-radius: 10px;
      color: white; font-size: 14px; font-weight: 600;
      cursor: pointer; transition: all 0.2s;
      display: flex; align-items: center; justify-content: center; gap: 8px;
    }
    .btn-new-property:hover { background: #1d4ed8; }

    /* MAIN */
    .main { margin-left: 260px; flex: 1; display: flex; flex-direction: column; }

    /* TOPBAR */
    .topbar {
      height: 64px; background: white;
      border-bottom: 1px solid #f1f5f9;
      padding: 0 32px;
      display: flex; align-items: center; justify-content: space-between;
      position: sticky; top: 0; z-index: 50;
      box-shadow: 0 1px 4px rgba(0,0,0,0.04);
    }
    .search-bar {
      display: flex; align-items: center; gap: 10px;
      background: #f8fafc; border: 1px solid #e2e8f0;
      border-radius: 10px; padding: 8px 16px;
      width: 320px;
    }
    .search-bar input {
      border: none; background: transparent; outline: none;
      font-size: 14px; color: #374151; width: 100%;
    }
    .search-bar input::placeholder { color: #94a3b8; }
    .topbar-right { display: flex; align-items: center; gap: 16px; }
    .icon-btn {
      width: 36px; height: 36px; border-radius: 8px;
      border: 1px solid #e2e8f0; background: white;
      display: flex; align-items: center; justify-content: center;
      cursor: pointer; font-size: 16px; transition: all 0.2s;
      position: relative;
    }
    .icon-btn:hover { background: #f8fafc; }
    .badge {
      position: absolute; top: -4px; right: -4px;
      width: 16px; height: 16px; background: #ef4444;
      border-radius: 50%; font-size: 9px; color: white;
      display: flex; align-items: center; justify-content: center;
      font-weight: 700;
    }
    .user-info { display: flex; align-items: center; gap: 10px; cursor: pointer; }
    .user-avatar {
      width: 36px; height: 36px; border-radius: 50%;
      background: #2563eb; color: white;
      display: flex; align-items: center; justify-content: center;
      font-size: 14px; font-weight: 700;
    }
    .user-name { font-size: 14px; font-weight: 600; color: #0f172a; }
    .user-role { font-size: 11px; color: #94a3b8; text-transform: uppercase; letter-spacing: 0.05em; }
    .divider { width: 1px; height: 24px; background: #e2e8f0; }

    /* CONTENT */
    .content { flex: 1; padding: 32px; }

    @media (max-width: 768px) {
      .sidebar { transform: translateX(-100%); }
      .main { margin-left: 0; }
    }
  `],
  template: `
    <div class="shell">

      <!-- SIDEBAR -->
      <aside class="sidebar">
        <div class="sidebar-brand">
          <div class="brand-title">🏠 Estatein</div>
          <div class="brand-sub">Asset Management</div>
        </div>

        <nav class="sidebar-nav">
          <a class="nav-item" routerLink="overview" routerLinkActive="active">
            <span class="nav-icon">📊</span> Overview
          </a>
          <a class="nav-item" routerLink="properties" routerLinkActive="active">
            <span class="nav-icon">🏢</span> My Properties
          </a>
          <a class="nav-item" routerLink="analytics" routerLinkActive="active">
            <span class="nav-icon">📈</span> Analytics
          </a>
          <a class="nav-item" routerLink="subscription" routerLinkActive="active">
            <span class="nav-icon">💳</span> Subscription
          </a>
          <a class="nav-item" routerLink="profile" routerLinkActive="active">
            <span class="nav-icon">👤</span> Profile
          </a>
        </nav>

        <div class="sidebar-bottom">
          <button class="btn-new-property" routerLink="properties/new">
            + New Property
          </button>
        </div>
      </aside>

      <!-- MAIN -->
      <div class="main">

        <!-- TOPBAR -->
        <header class="topbar">
          <div class="search-bar">
            <span>🔍</span>
            <input type="text" placeholder="Search assets, tenants, or reports..." />
          </div>
          <div class="topbar-right">
            <div class="icon-btn">
              🔔
              <span class="badge">4</span>
            </div>
            <div class="icon-btn">⚙️</div>
            <div class="divider"></div>
            <div class="user-info">
              <div class="user-avatar">{{ userInitial() }}</div>
              <div>
                <div class="user-name">{{ userName() }}</div>
                <div class="user-role">Premium Landlord</div>
              </div>
            </div>
          </div>
        </header>

        <!-- PAGE CONTENT -->
        <div class="content">
          <router-outlet />
        </div>

      </div>
    </div>
  `,
})
export class LandlordShellComponent {
  private auth = inject(AuthService);

 userName = () => this.auth.user()?.fullName ?? 'User';
userInitial = () => this.auth.user()?.fullName?.[0]?.toUpperCase() ?? 'U';
}