import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-broker-properties',
  standalone: true,
  imports: [CommonModule],
  template: `<h2>Assigned Properties</h2>
    <p class="placeholder">Properties assigned to this broker</p>`,
  styles: [
    `
      .placeholder {
        color: #95a5a6;
        font-style: italic;
      }
    `,
  ],
})
export class PropertiesComponent {}
