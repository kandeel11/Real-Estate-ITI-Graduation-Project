/**
 * Company Dashboard Shell
 * Sidebar layout for company dashboard pages: overview, properties, brokers,
 * performance, subscriptions, and settings.
 */

import { Component, inject } from '@angular/core';
import { Router, RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-company-dashboard',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './company-dashboard.component.html',
  styleUrl: './company-dashboard.component.css',
})
export class CompanyDashboardComponent {
  auth = inject(AuthService);
  private router = inject(Router);

  signOut(): void {
    this.auth.logout();
    this.router.navigate(['/auth/login']);
  }
}
