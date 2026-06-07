/**
 * Maintenance Company Reports Page
 */

import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-maintenance-reports',
  standalone: true,
  imports: [CommonModule],
  template: `<h2>Reports</h2><p class="placeholder">Maintenance analytics and performance reports</p>`,
  styles: [`.placeholder { color: #95a5a6; font-style: italic; }`],
})
export class ReportsComponent {}
