import { Component, inject } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive, Router } from '@angular/router';
import { NotificationService } from '../services/notification.service';
import { AuthService } from '../../../../core/services/auth.service';


interface NavItem {
  label: string;
  icon: string;
  route: string;
  exact?: boolean;
}

@Component({
  selector: 'app-t-dashboard',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './t-dashboard.component.html',
  styleUrl: './t-dashboard.component.css'
})
export class TDashboardComponent {


  protected readonly auth = inject(AuthService);
  protected readonly notificationService = inject(NotificationService);
  private readonly router = inject(Router);
  
    readonly navItems: NavItem[] = [
      { label: 'Overview',      icon: 'fa-solid fa-gauge-high',           route: '/dashboard',               exact: true },
      { label: 'My Property',   icon: 'fa-solid fa-building',             route: '/dashboard/property' },
      { label: 'My Lease',      icon: 'fa-solid fa-file-contract',        route: '/dashboard/lease' },
      { label: 'Maintenance',   icon: 'fa-solid fa-screwdriver-wrench',   route: '/dashboard/maintenance' },
      { label: 'Notifications', icon: 'fa-regular fa-bell',               route: '/dashboard/notifications' },
      { label: 'Ratings',       icon: 'fa-regular fa-star',               route: '/dashboard/ratings' },
      { label: 'Profile',       icon: 'fa-regular fa-circle-user',        route: '/dashboard/profile' },
      { label: 'AI Assistant',  icon: 'fa-solid fa-robot',                route: '/dashboard/ai' },
    ];
  
    get userName(): string {
      return this.auth.user()?.fullName ?? 'Tenant';
    }
  
    get userInitials(): string {
      const name = this.auth.user()?.fullName ?? '';
      return name
        .split(' ')
        .slice(0, 2)
        .map(w => w.charAt(0))
        .join('')
        .toUpperCase() || 'T';
    }
  
    logout(): void {
      this.auth.logout();
      this.router.navigate(['/auth/login']);
    }
}
