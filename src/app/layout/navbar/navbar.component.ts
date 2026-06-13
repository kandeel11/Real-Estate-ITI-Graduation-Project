import { Component, HostListener, computed, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../features/dashboards/tenant-dashboard/services/notification.service';

@Component({
  selector: 'app-navbar',
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './navbar.component.html',
  styleUrl: './navbar.component.css'
})
export class NavbarComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly notifService = inject(NotificationService);
  readonly authenticated = this.authService.authenticated;
  readonly user = this.authService.user;
  scrolled = signal(false);
  menuOpen = signal(false);

  /** Avatar dropdown is a tenant-only experience; other roles keep the plain logout button. */
  readonly isTenant = computed(() => this.authService.userRole() === 'tenant');

  /** Owners have no dashboard — their listings live on the public site. */
  readonly isOwner = computed(() => this.authService.userRole() === 'owner');

  readonly initials = computed(() => {
    const name = this.user()?.fullName?.trim() ?? '';
    const parts = name.split(/\s+/).filter(Boolean);
    const letters = (parts[0]?.[0] ?? '') + (parts[1]?.[0] ?? '');
    return letters.toUpperCase() || (this.user()?.email?.[0] ?? 'U').toUpperCase();
  });

  readonly dashboardRoute = computed(() =>
    this.authService.dashboardRouteFor(this.authService.userRole())
  );

  @HostListener('window:scroll')
  onScroll() {
    this.scrolled.set(window.scrollY > 10);
  }

  // Close the menu when clicking anywhere outside it
  @HostListener('document:click')
  onDocumentClick() {
    this.menuOpen.set(false);
  }

  toggleMenu(event: Event): void {
    event.stopPropagation();
    this.menuOpen.update((open) => !open);
  }

  closeMenu(): void {
    this.menuOpen.set(false);
  }

  logout(): void {
    this.menuOpen.set(false);
    this.authService.logout();
    this.router.navigate(['/']);
  }
}
