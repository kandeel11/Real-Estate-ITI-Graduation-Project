/**
 * Dashboard Layout Component
 * Master layout for all authenticated dashboards
 * Provides sidebar navigation, header, and main content area
 * Acts as a shell for the 5 role-specific dashboards
 */

import { Component, inject, signal } from '@angular/core';
import { RouterOutlet, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-dashboard-layout',
  standalone: true,
  imports: [RouterOutlet, CommonModule],
  template: `
    <div class="dashboard-layout">
      <!-- Sidebar Navigation -->
      <aside class="sidebar" [class.collapsed]="sidebarCollapsed()">
        <div class="sidebar-header">
          <h2>Dashboard</h2>
          <button
            class="toggle-btn"
            (click)="toggleSidebar()"
            title="Toggle sidebar"
          >
            ☰
          </button>
        </div>

        <nav class="sidebar-nav">
          <a
            *ngFor="let item of getMenuItems()"
            [href]="item.href"
            class="nav-item"
          >
            <span class="nav-icon">{{ item.icon }}</span>
            <span class="nav-label">{{ item.label }}</span>
          </a>
        </nav>

        <div class="sidebar-footer">
          <button (click)="logout()" class="logout-btn">Logout</button>
        </div>
      </aside>

      <!-- Main Content Area -->
      <div class="dashboard-main">
        <!-- Header -->
        <header class="dashboard-header">
          <div class="header-left">
            <h1>{{ getPageTitle() }}</h1>
          </div>
          <div class="header-right">
            <div class="user-profile">
              <span>{{ currentUser()?.firstName }}</span>
              <img
                [src]="currentUser()?.profileImage || '/assets/default-avatar.png'"
                alt="Avatar"
                class="avatar"
              />
            </div>
          </div>
        </header>

        <!-- Content -->
        <main class="dashboard-content">
          <router-outlet />
        </main>
      </div>
    </div>
  `,
  styles: [
    `
      .dashboard-layout {
        display: flex;
        min-height: 100vh;
        background-color: #f5f5f5;
      }

      /* ========== SIDEBAR ========== */
      .sidebar {
        width: 250px;
        background-color: #2c3e50;
        color: white;
        display: flex;
        flex-direction: column;
        box-shadow: 2px 0 5px rgba(0, 0, 0, 0.1);
        transition: width 0.3s ease;
      }

      .sidebar.collapsed {
        width: 70px;
      }

      .sidebar-header {
        padding: 1.5rem 1rem;
        border-bottom: 1px solid #34495e;
        display: flex;
        justify-content: space-between;
        align-items: center;
      }

      .sidebar-header h2 {
        margin: 0;
        font-size: 1.25rem;
      }

      .sidebar.collapsed .sidebar-header h2 {
        display: none;
      }

      .toggle-btn {
        background: none;
        border: none;
        color: white;
        font-size: 1.5rem;
        cursor: pointer;
        padding: 0;
      }

      .sidebar-nav {
        flex: 1;
        padding: 1rem 0;
        overflow-y: auto;
      }

      .nav-item {
        display: flex;
        align-items: center;
        gap: 1rem;
        padding: 1rem 1.5rem;
        color: white;
        text-decoration: none;
        transition: background-color 0.3s;
        border-left: 3px solid transparent;
      }

      .nav-item:hover {
        background-color: #34495e;
        border-left-color: #3498db;
      }

      .nav-icon {
        font-size: 1.25rem;
        min-width: 24px;
      }

      .nav-label {
        white-space: nowrap;
      }

      .sidebar.collapsed .nav-label {
        display: none;
      }

      .sidebar-footer {
        padding: 1rem;
        border-top: 1px solid #34495e;
      }

      .logout-btn {
        width: 100%;
        padding: 0.75rem 1rem;
        background-color: #e74c3c;
        border: none;
        color: white;
        border-radius: 4px;
        cursor: pointer;
        transition: background-color 0.3s;
      }

      .logout-btn:hover {
        background-color: #c0392b;
      }

      /* ========== MAIN CONTENT ========== */
      .dashboard-main {
        flex: 1;
        display: flex;
        flex-direction: column;
        overflow: hidden;
      }

      .dashboard-header {
        background-color: white;
        padding: 1.5rem 2rem;
        box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
        display: flex;
        justify-content: space-between;
        align-items: center;
      }

      .dashboard-header h1 {
        margin: 0;
        font-size: 1.5rem;
        color: #2c3e50;
      }

      .user-profile {
        display: flex;
        align-items: center;
        gap: 1rem;
      }

      .user-profile span {
        color: #2c3e50;
      }

      .avatar {
        width: 40px;
        height: 40px;
        border-radius: 50%;
        object-fit: cover;
        border: 2px solid #3498db;
      }

      .dashboard-content {
        flex: 1;
        padding: 2rem;
        overflow-y: auto;
      }

      /* ========== RESPONSIVE ========== */
      @media (max-width: 768px) {
        .sidebar {
          position: fixed;
          left: 0;
          top: 0;
          height: 100vh;
          z-index: 1000;
          width: 250px;
        }

        .sidebar.collapsed {
          width: 0;
          overflow: hidden;
        }

        .dashboard-main {
          margin-left: 0;
          width: 100%;
        }
      }
    `,
  ],
})
export class DashboardLayoutComponent {
  private authService = inject(AuthService);
  private router = inject(Router);

  readonly currentUser = this.authService.user;
  readonly sidebarCollapsed = signal(false);

  toggleSidebar(): void {
    this.sidebarCollapsed.update((val) => !val);
  }

  getMenuItems() {
    const role = this.authService.userRole();

    const menusByRole: Record<string, any[]> = {
      admin: [
        { icon: '📊', label: 'Overview', href: '/dashboard/admin/overview' },
        { icon: '👥', label: 'Users', href: '/dashboard/admin/users' },
        { icon: '🏢', label: 'Companies', href: '/dashboard/admin/companies' },
        {
          icon: '⚖️',
          label: 'Moderation',
          href: '/dashboard/admin/moderation',
        },
        { icon: '📈', label: 'Reports', href: '/dashboard/admin/reports' },
      ],
      company: [
        { icon: '📊', label: 'Overview', href: '/dashboard/company/overview' },
        {
          icon: '🏠',
          label: 'Properties',
          href: '/dashboard/company/properties',
        },
        { icon: '🤝', label: 'Brokers', href: '/dashboard/company/brokers' },
        { icon: '📋', label: 'Leases', href: '/dashboard/company/leases' },
        {
          icon: '💳',
          label: 'Subscriptions',
          href: '/dashboard/company/subscriptions',
        },
      ],
      broker: [
        { icon: '📊', label: 'Overview', href: '/dashboard/broker/overview' },
        {
          icon: '📑',
          label: 'Listings',
          href: '/dashboard/broker/listings',
        },
        {
          icon: '📈',
          label: 'Performance',
          href: '/dashboard/broker/performance',
        },
        {
          icon: '💰',
          label: 'Commissions',
          href: '/dashboard/broker/commissions',
        },
      ],
      tenant: [
        { icon: '📊', label: 'Overview', href: '/dashboard/tenant/overview' },
        {
          icon: '🏠',
          label: 'My Property',
          href: '/dashboard/tenant/my-property',
        },
        { icon: '📄', label: 'Lease', href: '/dashboard/tenant/lease' },
        {
          icon: '🔧',
          label: 'Maintenance',
          href: '/dashboard/tenant/maintenance-requests',
        },
        { icon: '💳', label: 'Payments', href: '/dashboard/tenant/payments' },
      ],
      'maintenance-company': [
        {
          icon: '📊',
          label: 'Overview',
          href: '/dashboard/maintenance-company/overview',
        },
        {
          icon: '🎫',
          label: 'Tickets',
          href: '/dashboard/maintenance-company/tickets',
        },
        {
          icon: '📋',
          label: 'Work Orders',
          href: '/dashboard/maintenance-company/work-orders',
        },
        {
          icon: '📊',
          label: 'Reports',
          href: '/dashboard/maintenance-company/reports',
        },
      ],
    };

    return menusByRole[role!] || [];
  }

  getPageTitle(): string {
    const role = this.authService.userRole();
    const titles: Record<string, string> = {
      admin: 'Admin Dashboard',
      company: 'Company Dashboard',
      broker: 'Broker Dashboard',
      tenant: 'Tenant Dashboard',
      'maintenance-company': 'Maintenance Dashboard',
    };
    return titles[role!] || 'Dashboard';
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/auth/login']);
  }
}
