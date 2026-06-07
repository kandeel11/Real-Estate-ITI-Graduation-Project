/**
 * Property Search Component
 * Advanced search and filtering for properties
 */

import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-property-search',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="property-search">
      <h1>Search Properties</h1>
      <p class="placeholder">Property Search Component - Advanced search with filters</p>
    </div>
  `,
  styles: [
    `
      .property-search {
        width: 100%;
      }
      .placeholder {
        color: #95a5a6;
        font-style: italic;
      }
    `,
  ],
})
export class PropertySearchComponent {}
