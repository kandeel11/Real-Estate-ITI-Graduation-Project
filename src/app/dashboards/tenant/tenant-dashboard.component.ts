/**
 * Tenant Dashboard Component
 * Main shell for tenant dashboard with access to property info, lease, maintenance requests, and payments
 */

import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-tenant-dashboard',
  standalone: true,
  imports: [RouterOutlet, CommonModule],
  template: `
    <div class="tenant-dashboard">
      <router-outlet />
    </div>
  `,
  styles: [
    `
      .tenant-dashboard {
        width: 100%;
      }
    `,
  ],
})
export class TenantDashboardComponent {}
