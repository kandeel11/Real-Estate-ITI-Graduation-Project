import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-broker-leads',
  standalone: true,
  imports: [CommonModule],
  template: `<h2>Leads Pipeline</h2>
    <p class="placeholder">Kanban board of leads and deals</p>`,
  styles: [
    `
      .placeholder {
        color: #95a5a6;
        font-style: italic;
      }
    `,
  ],
})
export class LeadsComponent {}
