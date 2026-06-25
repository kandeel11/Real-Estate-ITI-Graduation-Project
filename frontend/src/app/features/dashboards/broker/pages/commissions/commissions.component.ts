/**
 * Broker Commissions Page
 */

import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-broker-commissions',
  standalone: true,
  imports: [CommonModule],
  template: `<h2>Commissions</h2><p class="placeholder">Commission history and statements</p>`,
  styles: [`.placeholder { color: #95a5a6; font-style: italic; }`],
})
export class CommissionsComponent {}
