import { Component, inject, OnInit, computed } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive, Router } from '@angular/router';
import { trigger, transition, query, style, animate } from '@angular/animations';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../core/services/auth.service';
import { LanguageService } from '../../core/services/language.service';
import { NotificationService } from '../../core/services/notification.service';

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
  selector: 'app-broker-dashboard',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, TranslatePipe],
  templateUrl: './broker-dashboard.component.html',
  styleUrl: './broker-dashboard.component.css',
  animations: [routeAnimations],
})
export class BrokerDashboardComponent implements OnInit {
  protected readonly auth = inject(AuthService);
  protected readonly lang = inject(LanguageService);
  protected readonly notifications = inject(NotificationService);
  private readonly router = inject(Router);

  readonly isCompanyBroker = computed(() => !!this.auth.user()?.companyId);

  readonly navItems: NavItem[] = [
    { id: 'overview',     labelKey: 'BROKER_NAV.OVERVIEW',     icon: 'fa-solid fa-gauge-high',    route: '/dashboard/broker/overview' },
    { id: 'properties',  labelKey: 'BROKER_NAV.PROPERTIES',   icon: 'fa-solid fa-building',      route: '/dashboard/broker/properties' },
    { id: 'leads',       labelKey: 'BROKER_NAV.LEADS',        icon: 'fa-solid fa-filter',        route: '/dashboard/broker/leads' },
    { id: 'inquiries',   labelKey: 'BROKER_NAV.INQUIRIES',    icon: 'fa-regular fa-envelope',    route: '/dashboard/broker/inquiries' },
    { id: 'performance', labelKey: 'BROKER_NAV.PERFORMANCE',  icon: 'fa-solid fa-chart-line',    route: '/dashboard/broker/performance' },
    { id: 'leases',        labelKey: 'BROKER_NAV.LEASES',        icon: 'fa-solid fa-file-contract', route: '/dashboard/broker/leases' },
    { id: 'notifications', labelKey: 'BROKER_NAV.NOTIFICATIONS', icon: 'fa-regular fa-bell',       route: '/dashboard/broker/notifications',
      badge: () => this.notifications.unreadCount() },
    { id: 'subscription', labelKey: 'BROKER_NAV.SUBSCRIPTION', icon: 'fa-solid fa-crown',         route: '/dashboard/broker/subscription' },
    { id: 'profile',      labelKey: 'BROKER_NAV.PROFILE',      icon: 'fa-solid fa-user',          route: '/dashboard/broker/profile' },
  ];

  get userName(): string {
    return this.auth.user()?.fullName ?? 'Broker';
  }

  get userInitials(): string {
    const name = this.auth.user()?.fullName ?? '';
    return name.split(' ').slice(0, 2).map((w: string) => w.charAt(0)).join('').toUpperCase() || 'B';
  }

  get roleLabel(): string {
    return this.isCompanyBroker() ? 'BROKER_NAV.ROLE_COMPANY' : 'BROKER_NAV.ROLE_INDEPENDENT';
  }

  prepareRoute(outlet: RouterOutlet): string {
    return outlet?.isActivated
      ? (outlet.activatedRoute.snapshot.url.map(s => s.path).join('/') || 'root')
      : '';
  }

  ngOnInit(): void {
    // The app-root watcher keeps unreadCount fresh; refresh once on entry too.
    this.notifications.fetchUnreadCount();
  }

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/auth/login']);
  }
}
