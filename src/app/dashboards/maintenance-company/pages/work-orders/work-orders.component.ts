/**
 * Maintenance Company Work Orders Page
 */

import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-maintenance-work-orders',
  standalone: true,
  imports: [CommonModule],
  template: `<h2>Work Orders</h2><p class="placeholder">Create and track work orders for maintenance tasks</p>`,
  styles: [`.placeholder { color: #95a5a6; font-style: italic; }`],
})
export class WorkOrdersComponent {}
