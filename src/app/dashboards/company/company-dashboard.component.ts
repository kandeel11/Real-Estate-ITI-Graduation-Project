/**
 * Company Dashboard Component
 * Main shell for company dashboard with access to properties, brokers, leases, and subscriptions
 */

import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-company-dashboard',
  standalone: true,
  imports: [RouterOutlet, CommonModule],
  template: `
    <div class="company-dashboard">
      <router-outlet />
    </div>
  `,
  styles: [
    `
      .company-dashboard {
        width: 100%;
      }
    `,
  ],
})
export class CompanyDashboardComponent {}
