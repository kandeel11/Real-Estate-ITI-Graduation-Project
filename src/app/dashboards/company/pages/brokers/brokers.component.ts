/**
 * Company Brokers Page
 */

import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-company-brokers',
  standalone: true,
  imports: [CommonModule],
  template: `<h2>Brokers</h2><p class="placeholder">Manage company brokers and their performance</p>`,
  styles: [`.placeholder { color: #95a5a6; font-style: italic; }`],
})
export class BrokersComponent {}
