import { Component, input } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

interface NavItem {
  label: string;
  icon: string;
  route: string;
  badge?: number;
  badgeType?: 'urgent' | 'normal';
}

@Component({
  selector: 'app-maintenance-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './maintenance-sidebar.component.html',
  styleUrls: ['./maintenance-sidebar.component.scss'],
})
export class MaintenanceSidebarComponent {
  collapsed = input<boolean>(false);
  companyName = input<string>('Acme Maintenance');

  navItems: NavItem[] = [
    { label: 'Overview', icon: 'fa-solid fa-table-cells-large', route: '/maintenance/overview' },
    {
      label: 'Tickets',
      icon: 'fa-solid fa-ticket',
      route: '/maintenance/tickets',
      badge: 17,
      badgeType: 'urgent',
    },
    { label: 'Schedule', icon: 'fa-regular fa-calendar', route: '/maintenance/schedule' },
    { label: 'Customers', icon: 'fa-solid fa-users', route: '/maintenance/customers' },
    { label: 'Categories', icon: 'fa-solid fa-tags', route: '/maintenance/categories' },
    { label: 'Earnings', icon: 'fa-solid fa-sack-dollar', route: '/maintenance/earnings' },
    { label: 'Reports', icon: 'fa-solid fa-chart-column', route: '/maintenance/reports' },
    { label: 'Settings', icon: 'fa-solid fa-gear', route: '/maintenance/settings' },
  ];
}
