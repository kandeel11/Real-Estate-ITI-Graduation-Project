/**
 * Root Application Component
 * Bootstrap component for the entire Angular application
 * Serves as the entry point for all routes and layout components
 */

import { Component, effect, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AuthService } from './core/services/auth.service';
import { NotificationService } from './core/services/notification.service';
import { ToastComponent } from './shared/components/toast/toast.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, ToastComponent],
  template: `
    <router-outlet />
    <app-toast />
  `,
  styleUrls: [],
})
export class AppComponent {
  private readonly auth = inject(AuthService);
  private readonly notifications = inject(NotificationService);
  title = 'RealEstate Platform';

  constructor() {
    // App-wide notification toasts for every signed-in user (any role).
    effect(() => {
      if (this.auth.authenticated()) {
        this.notifications.startWatcher();
      } else {
        this.notifications.stopWatcher();
      }
    });
  }
}
