import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { MaintenanceSidebarComponent } from './maintenance-sidebar/maintenance-sidebar.component';

@Component({
  selector: 'app-maintenance-layout',
  standalone: true,
  imports: [RouterOutlet, MaintenanceSidebarComponent],
  templateUrl: './maintenance-layout.component.html',
  styleUrls: ['./maintenance-layout.component.scss'],
})
export class MaintenanceLayoutComponent {
  sidebarCollapsed = signal(false);
  toggleSidebar() {
    this.sidebarCollapsed.update((v) => !v);
  }
}
