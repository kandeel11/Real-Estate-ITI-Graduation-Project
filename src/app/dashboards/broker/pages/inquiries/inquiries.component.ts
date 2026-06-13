import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-broker-inquiries',
  standalone: true,
  imports: [CommonModule],
  template: `<h2>Inquiries</h2>
    <p class="placeholder">Incoming inquiry inbox</p>`,
  styles: [
    `
      .placeholder {
        color: #95a5a6;
        font-style: italic;
      }
    `,
  ],
})
export class InquiriesComponent {}
