/**
 * Broker Dashboard Overview Page
 */

import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-broker-overview',
  standalone: true,
  imports: [CommonModule],
  template: `<h2>Broker Overview</h2><p class="placeholder">Broker performance metrics and statistics</p>`,
  styles: [`.placeholder { color: #95a5a6; font-style: italic; }`],
})
export class OverviewComponent {}
