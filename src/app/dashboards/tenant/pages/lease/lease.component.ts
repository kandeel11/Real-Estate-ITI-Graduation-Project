/**
 * Tenant Lease Page
 */

import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-tenant-lease',
  standalone: true,
  imports: [CommonModule],
  template: `<h2>Lease</h2><p class="placeholder">View and manage lease document and terms</p>`,
  styles: [`.placeholder { color: #95a5a6; font-style: italic; }`],
})
export class LeaseComponent {}
