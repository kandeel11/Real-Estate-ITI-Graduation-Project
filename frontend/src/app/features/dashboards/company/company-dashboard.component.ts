/**
 * Company Dashboard Shell
 * Self-contained dashboard shell: fixed sidebar with active-route indicator,
 * user footer, route animations, and mobile bottom tab bar. A compact language
 * switcher lives in the sidebar brand row.
 */

import { Component, inject } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive, Router } from '@angular/router';
import { trigger, transition, query, style, animate } from '@angular/animations';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../../core/services/auth.service';
import { LanguageService } from '../../../core/services/language.service';

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
  selector: 'app-company-dashboard',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, TranslatePipe],
  templateUrl: './company-dashboard.component.html',
  styleUrl: './company-dashboard.component.css',
  animations: [routeAnimations],
})
export class CompanyDashboardComponent {
  protected readonly auth = inject(AuthService);
  protected readonly lang = inject(LanguageService);
  private readonly router = inject(Router);

  readonly navItems: NavItem[] = [
    { id: 'overview',      label: 'COMPANY_NAV.OVERVIEW',      icon: 'fa-solid fa-gauge-high',      route: '/dashboard/company/overview' },
    { id: 'pipeline',      label: 'COMPANY_NAV.PIPELINE',      icon: 'fa-solid fa-filter',          route: '/dashboard/company/pipeline' },
    { id: 'properties',    label: 'COMPANY_NAV.PROPERTIES',    icon: 'fa-solid fa-building',        route: '/dashboard/company/properties' },
    { id: 'brokers',       label: 'COMPANY_NAV.BROKERS',       icon: 'fa-solid fa-users',           route: '/dashboard/company/brokers' },
    { id: 'leases',        label: 'COMPANY_NAV.LEASES',        icon: 'fa-solid fa-file-contract',   route: '/dashboard/company/leases' },
    { id: 'tickets',       label: 'COMPANY_NAV.TICKETS',       icon: 'fa-solid fa-screwdriver-wrench', route: '/dashboard/company/tickets' },
    { id: 'payments',      label: 'COMPANY_NAV.PAYMENTS',      icon: 'fa-solid fa-credit-card',     route: '/dashboard/company/payments' },
    { id: 'performance',   label: 'COMPANY_NAV.PERFORMANCE',   icon: 'fa-solid fa-chart-line',      route: '/dashboard/company/performance' },
    { id: 'inquiries',     label: 'COMPANY_NAV.INQUIRIES',     icon: 'fa-regular fa-envelope',      route: '/dashboard/company/inquiries' },
    { id: 'ai',            label: 'COMPANY_NAV.AI_ASSISTANT',  icon: 'fa-solid fa-robot',           route: '/dashboard/company/ai' },
    { id: 'wallet',        label: 'COMPANY_NAV.WALLET',         icon: 'fa-solid fa-wallet',          route: '/dashboard/company/wallet' },
    { id: 'subscription',  label: 'COMPANY_NAV.SUBSCRIPTION',  icon: 'fa-regular fa-credit-card',   route: '/dashboard/company/subscriptions' },
    { id: 'settings',      label: 'COMPANY_NAV.SETTINGS',      icon: 'fa-solid fa-gear',            route: '/dashboard/company/settings' },
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
