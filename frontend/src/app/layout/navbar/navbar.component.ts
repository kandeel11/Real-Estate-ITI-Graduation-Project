import { Component, HostListener, computed, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService, AppNotification } from '../../features/dashboards/tenant-dashboard/services/notification.service';
import { LanguageService } from '../../core/services/language.service';
import { SearchOverlayComponent } from '../../shared/components/search-overlay/search-overlay.component';

@Component({
  selector: 'app-navbar',
  imports: [RouterLink, RouterLinkActive, TranslatePipe, SearchOverlayComponent],
  templateUrl: './navbar.component.html',
  styleUrl: './navbar.component.css'
})
export class NavbarComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  readonly lang = inject(LanguageService);

  protected readonly notifService = inject(NotificationService);
  readonly authenticated = this.authService.authenticated;
  readonly user = this.authService.user;

  scrolled      = signal(false);
  menuOpen      = signal(false);
  searchOpen    = signal(false);
  notifOpen     = signal(false);
  notifications = signal<AppNotification[]>([]);
  notifLoading  = signal(false);

  openSearch(): void  { this.searchOpen.set(true);  this.menuOpen.set(false); }
  closeSearch(): void { this.searchOpen.set(false); }

  readonly isTenant = computed(() => this.authService.userRole() === 'tenant');
  readonly isOwner  = computed(() => this.authService.userRole() === 'owner');

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
  onScroll() { this.scrolled.set(window.scrollY > 10); }

  @HostListener('document:click')
  onDocumentClick() {
    this.menuOpen.set(false);
    this.notifOpen.set(false);
  }

  toggleMenu(event: Event): void {
    event.stopPropagation();
    this.menuOpen.update(open => !open);
  }

  closeMenu(): void { this.menuOpen.set(false); }

  toggleNotif(event: Event): void {
    event.stopPropagation();
    const opening = !this.notifOpen();
    this.notifOpen.set(opening);
    this.menuOpen.set(false);
    if (opening && this.notifications().length === 0) {
      this.loadNotifications();
    }
  }

  private loadNotifications(): void {
    this.notifLoading.set(true);
    this.notifService.getNotifications(1, 8).subscribe({
      next: res => { this.notifications.set(res.items); this.notifLoading.set(false); },
      error: ()  => { this.notifications.set([]);       this.notifLoading.set(false); },
    });
  }

  onMarkAllRead(event: Event): void {
    event.stopPropagation();
    this.notifService.markAllRead().subscribe(() => {
      this.notifications.update(list => list.map(n => ({ ...n, isRead: true })));
      this.notifService.setUnreadCount(0);
    });
  }

  onNotifClick(n: AppNotification, event: Event): void {
    event.stopPropagation();
    if (!n.isRead) {
      this.notifService.markRead(n.id).subscribe(() => {
        this.notifications.update(list =>
          list.map(item => item.id === n.id ? { ...item, isRead: true } : item)
        );
        const remaining = this.notifications().filter(x => !x.isRead).length;
        this.notifService.setUnreadCount(remaining);
      });
    }
    this.notifOpen.set(false);
  }

  notifIcon(type: string): string {
    switch ((type ?? '').toLowerCase()) {
      case 'lease':       return 'fa-solid fa-file-contract';
      case 'payment':     return 'fa-solid fa-money-bill-wave';
      case 'maintenance': return 'fa-solid fa-wrench';
      case 'message':     return 'fa-solid fa-comment-dots';
      case 'property':    return 'fa-solid fa-house';
      default:            return 'fa-solid fa-bell';
    }
  }

  timeAgo(dateStr: string): string {
    const diff = Date.now() - new Date(dateStr).getTime();
    const m = Math.floor(diff / 60_000);
    if (m < 1)  return 'just now';
    if (m < 60) return `${m}m ago`;
    const h = Math.floor(m / 60);
    if (h < 24) return `${h}h ago`;
    const d = Math.floor(h / 24);
    return `${d}d ago`;
  }

  logout(): void {
    this.menuOpen.set(false);
    this.authService.logout();
    this.router.navigate(['/']);
  }
}
