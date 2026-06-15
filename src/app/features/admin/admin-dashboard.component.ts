/**
 * Admin Dashboard Component
 * Main shell for admin dashboard with access to user management, companies, moderation, and reporting
 */

import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [RouterOutlet, CommonModule],
  template: `
    <div class="admin-dashboard">
      <router-outlet />
    </div>
  `,
  styles: [
    `
      .admin-dashboard {
        width: 100%;
      }
    `,
  ],
})
export class AdminDashboardComponent {}
