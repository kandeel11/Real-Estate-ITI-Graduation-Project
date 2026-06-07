/**
 * Property List Component
 * Display paginated list of properties on public page
 */

import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-property-list',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="property-list">
      <h1>Featured Properties</h1>
      <p class="placeholder">Property List Component - Grid view of all available properties</p>
    </div>
  `,
  styles: [
    `
      .property-list {
        width: 100%;
      }
      .property-list h1 {
        color: #2c3e50;
      }
      .placeholder {
        color: #95a5a6;
        font-style: italic;
      }
    `,
  ],
})
export class PropertyListComponent {}
