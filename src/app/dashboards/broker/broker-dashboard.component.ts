/**
 * Broker Dashboard Component
 * Main shell for broker dashboard with access to listings, performance, and commissions
 */

import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-broker-dashboard',
  standalone: true,
  imports: [RouterOutlet, CommonModule],
  template: `
    <div class="broker-dashboard">
      <router-outlet />
    </div>
  `,
  styles: [
    `
      .broker-dashboard {
        width: 100%;
      }
    `,
  ],
})
export class BrokerDashboardComponent {}
