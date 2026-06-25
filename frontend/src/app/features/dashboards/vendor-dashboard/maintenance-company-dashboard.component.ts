/**
 * Maintenance Company Dashboard Component
 * Main shell for maintenance company dashboard with access to tickets, work orders, and reports
 */

import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-maintenance-company-dashboard',
  standalone: true,
  imports: [RouterOutlet, CommonModule],
  template: `
    <div class="maintenance-company-dashboard">
      <router-outlet />
    </div>
  `,
  styles: [
    `
      .maintenance-company-dashboard {
        width: 100%;
      }
    `,
  ],
})
export class MaintenanceCompanyDashboardComponent {}
