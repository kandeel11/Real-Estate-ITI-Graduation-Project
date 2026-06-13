import { Component, HostListener, computed, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../core/services/auth.service';
import { LanguageService } from '../../core/services/language.service';

@Component({
  selector: 'app-navbar',
  imports: [RouterLink, RouterLinkActive, TranslatePipe],
  templateUrl: './navbar.component.html',
  styleUrl: './navbar.component.css'
})
export class NavbarComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  readonly lang = inject(LanguageService);

  readonly authenticated = this.authService.authenticated;
  readonly user = this.authService.user;
  scrolled = signal(false);
  menuOpen = signal(false);

  readonly isTenant = computed(() => this.authService.userRole() === 'tenant');
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
