/**
 * Property Detail Component
 * Detailed view of a single property
 */

import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-property-detail',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="property-detail">
      <h1>Property Details</h1>
      <p class="placeholder">Property Detail Component - Full property information and images</p>
    </div>
  `,
  styles: [
    `
      .property-detail {
        width: 100%;
      }
      .placeholder {
        color: #95a5a6;
        font-style: italic;
      }
    `,
  ],
})
export class PropertyDetailComponent {}
