import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-broker-profile',
  standalone: true,
  imports: [CommonModule],
  template: `<h2>My Profile</h2>
    <p class="placeholder">Personal info, license, photo and availability</p>`,
  styles: [
    `
      .placeholder {
        color: #95a5a6;
        font-style: italic;
      }
    `,
  ],
})
export class ProfileComponent {}
