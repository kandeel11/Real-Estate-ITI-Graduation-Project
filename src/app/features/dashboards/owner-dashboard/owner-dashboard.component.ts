import { Component, inject, OnInit, computed, ChangeDetectorRef } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive, Router, NavigationEnd } from '@angular/router';
import { trigger, transition, query, style, animate } from '@angular/animations';
import { filter } from 'rxjs/operators';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../../core/services/auth.service';
import { LanguageService } from '../../../core/services/language.service';
import { NotificationService } from '../../../core/services/notification.service';

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
  labelKey: string;
  icon: string;
  route: string;
  badge?: () => number;
}

@Component({
  selector: 'app-owner-dashboard',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, TranslatePipe],
  templateUrl: './owner-dashboard.component.html',
  styleUrl: './owner-dashboard.component.css',
  animations: [routeAnimations],
})
export class OwnerDashboardComponent implements OnInit {
  protected readonly auth = inject(AuthService);
  protected readonly lang = inject(LanguageService);
  protected readonly notifications = inject(NotificationService);
  private readonly router = inject(Router);
  private readonly cdr = inject(ChangeDetectorRef);

  routeState = '';

  readonly navItems: NavItem[] = [
    { id: 'overview',     labelKey: 'OWNER_NAV.OVERVIEW',     icon: 'fa-solid fa-gauge-high',    route: '/dashboard/owner/overview' },
    { id: 'properties',  labelKey: 'OWNER_NAV.PROPERTIES',   icon: 'fa-solid fa-building',      route: '/dashboard/owner/properties' },
    { id: 'leads',       labelKey: 'OWNER_NAV.LEADS',        icon: 'fa-solid fa-filter',        route: '/dashboard/owner/leads' },
    { id: 'inquiries',   labelKey: 'OWNER_NAV.INQUIRIES',    icon: 'fa-regular fa-envelope',    route: '/dashboard/owner/inquiries' },
    { id: 'performance', labelKey: 'OWNER_NAV.PERFORMANCE',  icon: 'fa-solid fa-chart-line',    route: '/dashboard/owner/performance' },
    { id: 'leases',        labelKey: 'OWNER_NAV.LEASES',        icon: 'fa-solid fa-file-contract', route: '/dashboard/owner/leases' },
    { id: 'maintenance',   labelKey: 'OWNER_NAV.MAINTENANCE',   icon: 'fa-solid fa-screwdriver-wrench', route: '/dashboard/owner/maintenance' },
    { id: 'notifications', labelKey: 'OWNER_NAV.NOTIFICATIONS', icon: 'fa-regular fa-bell',       route: '/dashboard/owner/notifications',
      badge: () => this.notifications.unreadCount() },
    { id: 'subscription', labelKey: 'OWNER_NAV.SUBSCRIPTION', icon: 'fa-solid fa-crown',         route: '/dashboard/owner/subscription' },
    { id: 'profile',      labelKey: 'OWNER_NAV.SETTINGS',      icon: 'fa-solid fa-gear',          route: '/dashboard/owner/profile' },
  ];

  get userName(): string {
    return this.auth.user()?.fullName ?? 'Owner';
  }

  get userInitials(): string {
    const name = this.auth.user()?.fullName ?? '';
    return name.split(' ').slice(0, 2).map((w: string) => w.charAt(0)).join('').toUpperCase() || 'O';
  }

  get roleLabel(): string {
    return 'OWNER_NAV.ROLE';
  }

  ngOnInit(): void {
    this.notifications.fetchUnreadCount();
    // Update routeState from router events to avoid ExpressionChangedAfterItHasBeenCheckedError
    this.router.events
      .pipe(filter(e => e instanceof NavigationEnd))
      .subscribe((e) => {
        this.routeState = (e as NavigationEnd).urlAfterRedirects;
        this.cdr.detectChanges();
      });
  }

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/auth/login']);
  }
}
