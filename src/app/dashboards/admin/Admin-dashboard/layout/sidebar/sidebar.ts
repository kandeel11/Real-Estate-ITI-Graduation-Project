import { Component, input } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';

interface NavItem {
  labelKey: string;
  icon: string;
  route: string;
  badge?: number;
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, TranslatePipe],
  templateUrl: './sidebar.html',
  styleUrls: ['./sidebar.scss'],
})
export class SidebarComponent {
  collapsed = input<boolean>(false);

  navItems: NavItem[] = [
    { labelKey: 'ADMIN_NAV.DASHBOARD', icon: 'fa-solid fa-table-cells-large', route: 'overview' },
    { labelKey: 'ADMIN_NAV.PORTFOLIO', icon: 'fa-solid fa-building', route: 'properties' },
    { labelKey: 'ADMIN_NAV.MARKET_TRENDS', icon: 'fa-solid fa-chart-line', route: 'analytics' },
    { labelKey: 'ADMIN_NAV.TENANTS', icon: 'fa-solid fa-users', route: 'users' },
    { labelKey: 'ADMIN_NAV.COMPANIES', icon: 'fa-solid fa-briefcase', route: 'companies' },
    {
      labelKey: 'ADMIN_NAV.APPROVALS',
      icon: 'fa-solid fa-clipboard-check',
      route: 'approvals',
      badge: 17,
    },
    { labelKey: 'ADMIN_NAV.ANALYTICS', icon: 'fa-solid fa-chart-column', route: 'reports' },
    { labelKey: 'ADMIN_NAV.SETTINGS', icon: 'fa-solid fa-gear', route: 'settings' },
    {
      labelKey: 'ADMIN_NAV.MAINTENANCE',
      icon: 'fa-solid fa-screwdriver-wrench',
      route: 'maintenance-companies',
    },
  ];
}
