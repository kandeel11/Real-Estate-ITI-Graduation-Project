/**
 * Broker Listings Page
 */

import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-broker-listings',
  standalone: true,
  imports: [CommonModule],
  template: `<h2>My Listings</h2><p class="placeholder">Manage broker property listings</p>`,
  styles: [`.placeholder { color: #95a5a6; font-style: italic; }`],
})
export class ListingsComponent {}
