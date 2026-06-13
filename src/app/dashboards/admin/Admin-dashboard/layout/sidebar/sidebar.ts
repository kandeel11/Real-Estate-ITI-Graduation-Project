import { Component, inject, input } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../../../../core/services/auth.service';

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
  protected readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  navItems: NavItem[] = [
    { label: 'Dashboard', icon: 'fa-solid fa-table-cells-large', route: 'overview' },
    { label: 'Portfolio', icon: 'fa-solid fa-building', route: 'portfolio' },
    { label: 'Users', icon: 'fa-solid fa-users', route: 'users' },
    { label: 'Market Trends', icon: 'fa-solid fa-chart-line', route: 'market-trends' },

    // { label: 'Analytics', icon: 'fa-solid fa-chart-column', route: 'reports' },
    {
      label: 'Maintenance',
      icon: 'fa-solid fa-screwdriver-wrench',
      route: 'maintenance-companies',
    },
    { label: 'Settings', icon: 'fa-solid fa-gear', route: 'settings' },
  ];
  logout(): void {
    this.auth.logout();
    this.router.navigate(['/auth/login']);
  }
}
