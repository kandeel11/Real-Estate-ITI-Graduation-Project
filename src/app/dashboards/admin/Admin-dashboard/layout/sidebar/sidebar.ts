import { Component, input } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

interface NavItem {
  label: string;
  icon: string;
  route: string;
  badge?: number;
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './sidebar.html',
  styleUrls: ['./sidebar.scss'],
})
export class SidebarComponent {
  collapsed = input<boolean>(false);

  navItems: NavItem[] = [
    { label: 'Dashboard', icon: 'fa-solid fa-table-cells-large', route: 'Overview' },
    { label: 'Portfolio', icon: 'fa-solid fa-building', route: 'properties' },
    { label: 'Market Trends', icon: 'fa-solid fa-chart-line', route: 'analytics' },
    { label: 'Tenants', icon: 'fa-solid fa-users', route: 'users' },
    { label: 'Companies', icon: 'fa-solid fa-briefcase', route: 'companies' },
    {
      label: 'Approvals',
      icon: 'fa-solid fa-clipboard-check',
      route: 'approvals',
      badge: 17,
    },
    { label: 'Analytics', icon: 'fa-solid fa-chart-column', route: 'reports' },
    { label: 'Settings', icon: 'fa-solid fa-gear', route: 'settings' },
    {
      label: 'Maintenance',
      icon: 'fa-solid fa-screwdriver-wrench',
      route: 'maintenance-companies',
    },
  ];
}
