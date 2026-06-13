import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive, Router, NavigationEnd } from '@angular/router';
import { trigger, transition, query, style, animate } from '@angular/animations';
import { filter, Subscription } from 'rxjs';
import { TranslatePipe } from '@ngx-translate/core';
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
  id: string;
  label: string;
  icon: string;
  route: string;
  exact?: boolean;
}

@Component({
  selector: 'app-t-dashboard',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ToastComponent, NavbarComponent, TranslatePipe],
  templateUrl: './t-dashboard.component.html',
  styleUrl: './t-dashboard.component.css',
  animations: [routeAnimations],
})
export class TDashboardComponent implements OnInit, OnDestroy {
  protected readonly auth = inject(AuthService);
  protected readonly notificationService = inject(NotificationService);
  private readonly router = inject(Router);

  protected readonly routeAnimState = signal('');
  private _routerSub: Subscription | null = null;

  readonly navItems: NavItem[] = [
    { id: 'overview',      label: 'TENANT_NAV.OVERVIEW',      icon: 'fa-solid fa-gauge-high',         route: '/dashboard',               exact: true },
    { id: 'property',      label: 'TENANT_NAV.MY_PROPERTY',   icon: 'fa-solid fa-building',           route: '/dashboard/property' },
    { id: 'lease',         label: 'TENANT_NAV.MY_LEASE',      icon: 'fa-solid fa-file-contract',      route: '/dashboard/lease' },
    { id: 'maintenance',   label: 'TENANT_NAV.MAINTENANCE',   icon: 'fa-solid fa-screwdriver-wrench', route: '/dashboard/maintenance' },
    { id: 'notifications', label: 'TENANT_NAV.NOTIFICATIONS', icon: 'fa-regular fa-bell',             route: '/dashboard/notifications' },
    { id: 'subscription',  label: 'TENANT_NAV.SUBSCRIPTION',  icon: 'fa-solid fa-crown',              route: '/dashboard/subscription' },
    { id: 'ratings',       label: 'TENANT_NAV.RATINGS',       icon: 'fa-regular fa-star',             route: '/dashboard/ratings' },
    { id: 'profile',       label: 'TENANT_NAV.PROFILE',       icon: 'fa-regular fa-circle-user',      route: '/dashboard/profile' },
    { id: 'ai',            label: 'TENANT_NAV.AI_ASSISTANT',  icon: 'fa-solid fa-robot',              route: '/dashboard/ai' },
  ];

  ngOnInit(): void {
    this.notificationService.fetchUnreadCount();
    this.notificationService.startPolling();
    this._routerSub = this.router.events.pipe(
      filter(e => e instanceof NavigationEnd)
    ).subscribe(() => {
      const segments = this.router.url.split('/').filter(Boolean);
      this.routeAnimState.set(segments[segments.length - 1] || 'root');
    });
  }

  ngOnDestroy(): void {
    this.notificationService.stopPolling();
    this._routerSub?.unsubscribe();
  }

  get userName(): string {
    return this.auth.user()?.fullName ?? 'Tenant';
  }

  get userInitials(): string {
    const name = this.auth.user()?.fullName ?? '';
    return name.split(' ').slice(0, 2).map(w => w.charAt(0)).join('').toUpperCase() || 'T';
  }

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/auth/login']);
  }
}
