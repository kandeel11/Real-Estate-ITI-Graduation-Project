/**
 * Tenant Maintenance Requests Page
 */

import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-tenant-maintenance-requests',
  standalone: true,
  imports: [CommonModule],
  template: `<h2>Maintenance Requests</h2><p class="placeholder">Submit and track maintenance requests</p>`,
  styles: [`.placeholder { color: #95a5a6; font-style: italic; }`],
})
export class MaintenanceRequestsComponent {}
