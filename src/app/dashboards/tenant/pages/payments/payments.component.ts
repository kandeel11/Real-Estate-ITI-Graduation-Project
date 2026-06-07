/**
 * Tenant Payments Page
 */

import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-tenant-payments',
  standalone: true,
  imports: [CommonModule],
  template: `<h2>Payments</h2><p class="placeholder">Rent payment history and invoices</p>`,
  styles: [`.placeholder { color: #95a5a6; font-style: italic; }`],
})
export class PaymentsComponent {}
