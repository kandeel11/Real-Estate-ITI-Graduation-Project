import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive, Router } from '@angular/router';
import { trigger, transition, query, style, animate } from '@angular/animations';
import { NotificationService } from '../services/notification.service';
import { AuthService } from '../../../../core/services/auth.service';
import { ToastComponent } from '../../../../shared/components/toast/toast.component';
import { NavbarComponent } from '../../../../layout/navbar/navbar.component';

// eslint-disable-next-line @typescript-eslint/no-explicit-any
const routeAnimations = trigger('routeAnimations', [
  transition('* <=> *', [
    query(':enter', [
      style({ opacity: 0, transform: 'translateY(14px)' }),
      animate('220ms ease-out', style({ opacity: 1, transform: 'translateY(0)' })),
    ], { optional: true }),
  ]),
]);

interface NavItem {
  label: string;
  icon: string;
  route: string;
  exact?: boolean;
}

@Component({
  selector: 'app-t-dashboard',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ToastComponent, NavbarComponent],
  templateUrl: './t-dashboard.component.html',
  styleUrl: './t-dashboard.component.css',
  animations: [routeAnimations],
})
export class TDashboardComponent implements OnInit, OnDestroy {
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

  ngOnInit(): void {
    this.notificationService.fetchUnreadCount();
    this.notificationService.startPolling();
  }

  ngOnDestroy(): void {
    this.notificationService.stopPolling();
  }

  get userName(): string {
    return this.auth.user()?.fullName ?? 'Tenant';
  }

  get userInitials(): string {
    const name = this.auth.user()?.fullName ?? '';
    return name.split(' ').slice(0, 2).map(w => w.charAt(0)).join('').toUpperCase() || 'T';
  }

  prepareRoute(outlet: RouterOutlet): string {
    return outlet?.isActivated
      ? (outlet.activatedRoute.snapshot.url.map(s => s.path).join('/') || 'root')
      : '';
  }

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/auth/login']);
  }
}
