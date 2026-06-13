/**
 * Company Dashboard Shell
 * Same design system as the tenant dashboard (fixed sidebar with active-route
 * indicator, user footer, route animations, mobile bottom tab bar) — but
 * without the public site navbar: company managers land here directly after
 * login, so the shell owns the full viewport.
 */

import { Component, inject } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive, Router } from '@angular/router';
import { trigger, transition, query, style, animate } from '@angular/animations';
import { AuthService } from '../../core/services/auth.service';

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
  selector: 'app-company-dashboard',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './company-dashboard.component.html',
  styleUrl: './company-dashboard.component.css',
  animations: [routeAnimations],
})
export class CompanyDashboardComponent {
  protected readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly navItems: NavItem[] = [
    { label: 'Overview',     icon: 'fa-solid fa-gauge-high',     route: '/dashboard/company/overview' },
    { label: 'Properties',   icon: 'fa-solid fa-building',       route: '/dashboard/company/properties' },
    { label: 'Brokers',      icon: 'fa-solid fa-users',          route: '/dashboard/company/brokers' },
    { label: 'Performance',  icon: 'fa-solid fa-chart-line',     route: '/dashboard/company/performance' },
    { label: 'Inquiries',    icon: 'fa-regular fa-envelope',     route: '/dashboard/company/inquiries' },
    { label: 'AI Assistant', icon: 'fa-solid fa-robot',          route: '/dashboard/company/ai' },
    { label: 'Subscription', icon: 'fa-regular fa-credit-card',  route: '/dashboard/company/subscriptions' },
    { label: 'Settings',     icon: 'fa-solid fa-gear',           route: '/dashboard/company/settings' },
  ];

  get userName(): string {
    return this.auth.user()?.fullName ?? 'Manager';
  }

  get userInitials(): string {
    const name = this.auth.user()?.fullName ?? '';
    return name.split(' ').slice(0, 2).map(w => w.charAt(0)).join('').toUpperCase() || 'M';
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
