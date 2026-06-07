/**
 * Tenant Dashboard Overview Page
 */

import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-tenant-overview',
  standalone: true,
  imports: [CommonModule],
  template: `<h2>Tenant Overview</h2><p class="placeholder">Quick access to lease info and payments</p>`,
  styles: [`.placeholder { color: #95a5a6; font-style: italic; }`],
})
export class OverviewComponent {}
