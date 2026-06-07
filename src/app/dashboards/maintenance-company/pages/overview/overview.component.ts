/**
 * Maintenance Company Dashboard Overview Page
 */

import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-maintenance-overview',
  standalone: true,
  imports: [CommonModule],
  template: `<h2>Maintenance Overview</h2><p class="placeholder">Ticket metrics and team performance</p>`,
  styles: [`.placeholder { color: #95a5a6; font-style: italic; }`],
})
export class OverviewComponent {}
