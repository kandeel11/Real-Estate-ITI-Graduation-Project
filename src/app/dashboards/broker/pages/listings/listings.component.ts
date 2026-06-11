/**
 * Broker Listings Page
 */

import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-broker-listings',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="d-flex justify-content-between align-items-center mb-3">
      <h2 class="mb-0">My Listings</h2>
      <a routerLink="/properties/new" class="btn btn-dark">
        <i class="fa-solid fa-plus me-2"></i>Add Listing
      </a>
    </div>
    <p class="placeholder-text">Manage broker property listings</p>
  `,
  styles: [`.placeholder-text { color: #95a5a6; font-style: italic; }`],
})
export class ListingsComponent {}
