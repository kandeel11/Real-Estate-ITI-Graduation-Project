/**
 * Admin Dashboard Overview Page
 * Admin metrics, system statistics, and quick actions
 */

import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-admin-overview',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="admin-overview">
      <h2>Admin Overview</h2>
      <p class="placeholder">Dashboard overview with key metrics and statistics</p>
    </div>
  `,
  styles: [
    `
      .placeholder {
        color: #95a5a6;
        font-style: italic;
      }
    `,
  ],
})
export class OverviewComponent {}
