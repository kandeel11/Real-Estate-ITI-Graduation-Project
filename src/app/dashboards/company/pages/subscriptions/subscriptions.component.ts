/**
 * Company Subscriptions Page
 */

import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-company-subscriptions',
  standalone: true,
  imports: [CommonModule],
  template: `<h2>Subscriptions</h2><p class="placeholder">Manage subscription plans and billing</p>`,
  styles: [`.placeholder { color: #95a5a6; font-style: italic; }`],
})
export class SubscriptionsComponent {}
