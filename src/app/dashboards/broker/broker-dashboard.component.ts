import { Component, signal } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';

interface NavItem {
  label: string;
  icon: string;
  route: string;
}

@Component({
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, CommonModule],
  styles: [
    `
      .shell {
        display: flex;
        min-height: 100vh;
        background: #f8f9fa;
      }
      .sidebar {
        width: 240px;
        background: #1b4332;
        color: #fff;
        display: flex;
        flex-direction: column;
        flex-shrink: 0;
        transition: width 0.25s ease;
        overflow: hidden;
      }
      .sidebar.collapsed {
        width: 64px;
      }
      .sidebar-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        padding: 1.25rem 1rem;
        border-bottom: 1px solid #2d6a4f;
        min-height: 64px;
      }
      .brand {
        font-size: 1rem;
        font-weight: 700;
        color: #a7f3d0;
        white-space: nowrap;
        overflow: hidden;
      }
      .toggle-btn {
        background: none;
        border: none;
        color: #a7f3d0;
        cursor: pointer;
        font-size: 1.2rem;
        padding: 0;
        flex-shrink: 0;
      }
      .toggle-btn:hover {
        color: #fff;
      }
      .nav {
        flex: 1;
        padding: 1rem 0;
      }
      .nav-item {
        display: flex;
        align-items: center;
        gap: 0.85rem;
        padding: 0.75rem 1rem;
        color: #a7f3d0;
        text-decoration: none;
        font-size: 0.9rem;
        font-weight: 500;
        transition: background 0.15s;
        white-space: nowrap;
        border-left: 3px solid transparent;
      }
      .nav-item:hover {
        background: #2d6a4f;
        color: #fff;
      }
      .nav-item.active {
        background: #2d6a4f;
        color: #fff;
        border-left-color: #a7f3d0;
      }
      .nav-icon {
        font-size: 1.1rem;
        flex-shrink: 0;
        width: 20px;
        text-align: center;
      }
      .nav-label {
        overflow: hidden;
        transition: opacity 0.2s;
      }
      .sidebar.collapsed .nav-label {
        opacity: 0;
        width: 0;
      }
      .sidebar.collapsed .brand {
        opacity: 0;
        width: 0;
      }
      .badge {
        background: #ef4444;
        color: #fff;
        font-size: 0.68rem;
        font-weight: 700;
        padding: 0.1rem 0.45rem;
        border-radius: 20px;
        margin-left: auto;
        flex-shrink: 0;
      }
      .sidebar.collapsed .badge {
        display: none;
      }
      .sidebar-footer {
        padding: 1rem;
        border-top: 1px solid #2d6a4f;
      }
      .user-info {
        display: flex;
        align-items: center;
        gap: 0.75rem;
      }
      .avatar {
        width: 36px;
        height: 36px;
        border-radius: 50%;
        background: #2d6a4f;
        display: flex;
        align-items: center;
        justify-content: center;
        font-size: 1rem;
        flex-shrink: 0;
      }
      .user-details {
        overflow: hidden;
      }
      .user-name {
        font-size: 0.85rem;
        font-weight: 600;
        color: #fff;
        white-space: nowrap;
      }
      .user-role {
        font-size: 0.75rem;
        color: #a7f3d0;
      }
      .sidebar.collapsed .user-details {
        display: none;
      }
      .main {
        flex: 1;
        display: flex;
        flex-direction: column;
        min-width: 0;
      }
      .topbar {
        background: #fff;
        border-bottom: 1px solid #e9ecef;
        padding: 0 1.5rem;
        height: 64px;
        display: flex;
        align-items: center;
        justify-content: space-between;
        box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);
      }
      .topbar-title {
        font-size: 1rem;
        font-weight: 600;
        color: #1a1a2e;
      }
      .topbar-actions {
        display: flex;
        align-items: center;
        gap: 1rem;
      }
      .notif-btn {
        background: none;
        border: none;
        font-size: 1.2rem;
        cursor: pointer;
        position: relative;
      }
      .notif-dot {
        position: absolute;
        top: 0;
        right: 0;
        width: 8px;
        height: 8px;
        background: #ef4444;
        border-radius: 50%;
        border: 2px solid #fff;
      }
      .content {
        flex: 1;
        overflow-y: auto;
      }
      @media (max-width: 768px) {
        .shell {
          flex-direction: column;
        }
        .sidebar {
          width: 100%;
          flex-direction: row;
          flex-wrap: wrap;
          min-height: auto;
        }
        .sidebar.collapsed {
          width: 100%;
        }
        .nav {
          display: flex;
          flex-direction: row;
          padding: 0;
          overflow-x: auto;
        }
        .nav-item {
          padding: 0.6rem 0.75rem;
          border-left: none;
          border-bottom: 3px solid transparent;
        }
        .nav-item.active {
          border-left-color: transparent;
          border-bottom-color: #a7f3d0;
        }
        .sidebar-footer {
          display: none;
        }
        .sidebar-header {
          width: 100%;
        }
      }
    `,
  ],
  template: `
    <div class="shell">
      <aside class="sidebar" [class.collapsed]="collapsed()">
        <div class="sidebar-header">
          <span class="brand">🏠 Broker</span>
          <button class="toggle-btn" (click)="toggleSidebar()">
            {{ collapsed() ? '→' : '←' }}
          </button>
        </div>
        <nav class="nav">
          @for (item of navItems; track item.route) {
            <a class="nav-item" [routerLink]="item.route" routerLinkActive="active">
              <span class="nav-icon">{{ item.icon }}</span>
              <span class="nav-label">{{ item.label }}</span>
              @if (item.label === 'Inquiries') {
                <span class="badge">3</span>
              }
            </a>
          }
        </nav>
        <div class="sidebar-footer">
          <div class="user-info">
            <div class="avatar">👤</div>
            <div class="user-details">
              <div class="user-name">Sara Hassan</div>
              <div class="user-role">Broker</div>
            </div>
          </div>
        </div>
      </aside>
      <div class="main">
        <header class="topbar">
          <span class="topbar-title">Broker Dashboard</span>
          <div class="topbar-actions">
            <button class="notif-btn">
              🔔
              <span class="notif-dot"></span>
            </button>
          </div>
        </header>
        <div class="content">
          <router-outlet />
        </div>
      </div>
    </div>
  `,
})
export class BrokerDashboardComponent {
  collapsed = signal(false);

  navItems: NavItem[] = [
    { label: 'Overview', icon: '📊', route: 'overview' },
    { label: 'Properties', icon: '🏠', route: 'properties' },
    { label: 'Leads', icon: '🎯', route: 'leads' },
    { label: 'Inquiries', icon: '📩', route: 'inquiries' },
    { label: 'Performance', icon: '📈', route: 'performance' },
    { label: 'Profile', icon: '👤', route: 'profile' },
  ];

  toggleSidebar() {
    this.collapsed.update((v) => !v);
  }
}
