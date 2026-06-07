/**
 * Company Dashboard Overview Page
 */

import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-company-overview',
  standalone: true,
  imports: [CommonModule],
  template: `<h2>Company Overview</h2><p class="placeholder">Company metrics and quick actions</p>`,
  styles: [`.placeholder { color: #95a5a6; font-style: italic; }`],
})
export class OverviewComponent {}
