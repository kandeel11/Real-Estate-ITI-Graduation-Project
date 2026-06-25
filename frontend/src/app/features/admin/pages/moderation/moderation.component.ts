/**
 * Moderation Page
 * Admin panel for handling reported content and user violations
 */

import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-moderation',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="moderation">
      <h2>Moderation</h2>
      <p class="placeholder">Review reported content, manage violations, and take enforcement actions</p>
    </div>
  `,
  styles: [
    `
      .placeholder {
        color: #95a5a6;
        font-style: italic;
      }
    `,
  ],
})
export class ModerationComponent {}
