import { Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SidebarComponent } from './sidebar/sidebar';
import { TopbarComponent } from './topbar/topbar';
import { LanguageService } from '../../../../core/services/language.service';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [RouterOutlet, SidebarComponent, TopbarComponent],
  templateUrl: './admin-layout.html',
  styleUrls: ['./admin-layout.scss'],
})
export class AdminLayoutComponent {
  readonly lang = inject(LanguageService);
  sidebarCollapsed  = signal(false);
  mobileSidebarOpen = signal(false);

  toggleSidebar() {
    if (typeof window !== 'undefined' && window.innerWidth <= 768) {
      this.mobileSidebarOpen.update((v) => !v);
    } else {
      this.sidebarCollapsed.update((v) => !v);
    }
  }

  closeMobileSidebar() {
    this.mobileSidebarOpen.set(false);
  }
}
