/**
 * Public Layout Component
 * Wrapper for public routes (home, search, property details)
 * No authentication required
 */

import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-public-layout',
  standalone: true,
  imports: [RouterOutlet, CommonModule],
  template: `
    <div class="public-layout">
      <!-- Navigation Bar -->
      <nav class="navbar">
        <div class="navbar-brand">RealEstate Platform</div>
        <div class="navbar-links">
          <a href="/">Home</a>
          <a href="/properties/search">Search</a>
          <a href="/ai/chat">AI Chat</a>
          <a href="/auth/login">Login</a>
        </div>
      </nav>

      <!-- Main Content -->
      <main class="main-content">
        <router-outlet />
      </main>

      <!-- Footer -->
      <footer class="footer">
        <p>&copy; 2026 RealEstate Platform. All rights reserved.</p>
      </footer>
    </div>
  `,
  styles: [
    `
      .public-layout {
        display: flex;
        flex-direction: column;
        min-height: 100vh;
      }

      .navbar {
        background-color: #f8f9fa;
        padding: 1rem 2rem;
        border-bottom: 1px solid #dee2e6;
        display: flex;
        justify-content: space-between;
        align-items: center;
      }

      .navbar-brand {
        font-size: 1.5rem;
        font-weight: bold;
      }

      .navbar-links {
        display: flex;
        gap: 2rem;
      }

      .navbar-links a {
        text-decoration: none;
        color: #495057;
        transition: color 0.3s;
      }

      .navbar-links a:hover {
        color: #0d6efd;
      }

      .main-content {
        flex: 1;
        padding: 2rem;
      }

      .footer {
        background-color: #f8f9fa;
        border-top: 1px solid #dee2e6;
        padding: 2rem;
        text-align: center;
        color: #6c757d;
      }
    `,
  ],
})
export class PublicLayoutComponent {}
