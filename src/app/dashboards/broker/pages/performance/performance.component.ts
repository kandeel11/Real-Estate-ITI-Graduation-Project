/**
 * Broker Performance Page
 */

import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-broker-performance',
  standalone: true,
  imports: [CommonModule],
  template: `<h2>Performance</h2><p class="placeholder">Sales, views, and conversion metrics</p>`,
  styles: [`.placeholder { color: #95a5a6; font-style: italic; }`],
})
export class PerformanceComponent {}
