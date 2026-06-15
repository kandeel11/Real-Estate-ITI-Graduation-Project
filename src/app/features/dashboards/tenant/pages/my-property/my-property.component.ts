/**
 * Tenant My Property Page
 */

import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-tenant-my-property',
  standalone: true,
  imports: [CommonModule],
  template: `<h2>My Property</h2><p class="placeholder">Property details and rental information</p>`,
  styles: [`.placeholder { color: #95a5a6; font-style: italic; }`],
})
export class MyPropertyComponent {}
