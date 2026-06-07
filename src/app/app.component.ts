/**
 * Root Application Component
 * Bootstrap component for the entire Angular application
 * Serves as the entry point for all routes and layout components
 */

import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  template: `<router-outlet />`,
  styleUrls: [],
})
export class AppComponent {
  title = 'RealEstate Platform';
}
