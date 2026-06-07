/**
 * Maintenance Company Tickets Page
 */

import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-maintenance-tickets',
  standalone: true,
  imports: [CommonModule],
  template: `<h2>Tickets</h2><p class="placeholder">Maintenance ticket management and assignment</p>`,
  styles: [`.placeholder { color: #95a5a6; font-style: italic; }`],
})
export class TicketsComponent {}
