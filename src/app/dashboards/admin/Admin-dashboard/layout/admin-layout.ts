import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SidebarComponent } from './sidebar/sidebar';
import { TopbarComponent } from './topbar/topbar';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [RouterOutlet, SidebarComponent, TopbarComponent],
  templateUrl: './admin-layout.html',
  styleUrls: ['./admin-layout.scss'],
})
export class AdminLayoutComponent {
  sidebarCollapsed = signal(false);

  toggleSidebar() {
    this.sidebarCollapsed.update((v) => !v);
  }
}
