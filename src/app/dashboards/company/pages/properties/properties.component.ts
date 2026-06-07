/**
 * Company Properties Page
 */

import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-company-properties',
  standalone: true,
  imports: [CommonModule],
  template: `<h2>Properties</h2><p class="placeholder">Manage company properties CRUD operations</p>`,
  styles: [`.placeholder { color: #95a5a6; font-style: italic; }`],
})
export class PropertiesComponent {}
