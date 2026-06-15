import { Component, inject, input } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../../../../../src/app/core/services/auth.service';

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
  companyName = input<string>('Maintenance Co.');

  private auth = inject(AuthService);
  private router = inject(Router);

  navItems: NavItem[] = [
    {
      label: 'Overview',
      icon: 'fa-solid fa-table-cells-large',
      route: '/dashboard/maintenance/overview',
    },
    { label: 'Tickets', icon: 'fa-solid fa-ticket', route: '/dashboard/maintenance/tickets' },
  ];

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/auth/login']);
  }
}
