/**
 * Users Management Page
 * Admin panel for managing all platform users
 */

import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-users-management',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="users-management">
      <h2>Users Management</h2>
      <p class="placeholder">User table with filtering, search, and actions</p>
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
export class UsersManagementComponent {}
