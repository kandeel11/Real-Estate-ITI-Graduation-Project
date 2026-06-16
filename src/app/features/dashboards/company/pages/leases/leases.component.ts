/**
 * Company Leases Page
 */

import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-company-leases',
  standalone: true,
  imports: [CommonModule],
  template: `<h2>Leases</h2><p class="placeholder">Manage active and archived leases</p>`,
  styles: [`.placeholder { color: #95a5a6; font-style: italic; }`],
})
export class LeasesComponent {}
