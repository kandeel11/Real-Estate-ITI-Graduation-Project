import { Component, signal, inject } from '@angular/core';
import { RouterOutlet, Router, NavigationEnd } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { filter } from 'rxjs';
import { NavbarComponent } from '../navbar/navbar.component';
import { FooterComponent } from '../footer/footer.component';
import { ChatWidgetComponent } from '../../features/ai/ChatBot/chat-widget-component/chat-widget-component';

/** Routes that render without the public chrome (focused, full-screen flows). */
const CHROME_HIDDEN_ROUTES = ['/properties/new'];

@Component({
  selector: 'app-main-layout',
  imports: [RouterOutlet, NavbarComponent, FooterComponent, ChatWidgetComponent],
  templateUrl: './main-layout.component.html',
  styleUrl: './main-layout.component.css',
})
export class MainLayoutComponent {
  private readonly router = inject(Router);

  /** Hide the navbar, footer and chat widget on focused flows like the wizard. */
  protected readonly hideChrome = signal(this.shouldHide(this.router.url));

  constructor() {
    this.router.events
      .pipe(
        filter((e): e is NavigationEnd => e instanceof NavigationEnd),
        takeUntilDestroyed(),
      )
      .subscribe((e) => this.hideChrome.set(this.shouldHide(e.urlAfterRedirects)));
  }

  private shouldHide(url: string): boolean {
    const path = url.split('?')[0].split('#')[0].replace(/\/+$/, '');
    return CHROME_HIDDEN_ROUTES.includes(path);
  }
}
