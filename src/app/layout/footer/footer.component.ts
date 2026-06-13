import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-footer',
  imports: [RouterLink, TranslatePipe],
  templateUrl: './footer.component.html',
  styleUrl: './footer.component.css'
})
export class FooterComponent {
  currentYear = new Date().getFullYear();

  homeLinks = [
    { labelKey: 'FOOTER.HOME_LINKS.HERO',         path: '/' },
    { labelKey: 'FOOTER.HOME_LINKS.FEATURES',     path: '/' },
    { labelKey: 'FOOTER.HOME_LINKS.PROPERTIES',   path: '/properties' },
    { labelKey: 'FOOTER.HOME_LINKS.TESTIMONIALS', path: '/' },
    { labelKey: 'FOOTER.HOME_LINKS.FAQS',         path: '/' },
  ];

  aboutLinks = [
    { labelKey: 'FOOTER.ABOUT_LINKS.OUR_STORY',    path: '/about' },
    { labelKey: 'FOOTER.ABOUT_LINKS.OUR_WORKS',    path: '/about' },
    { labelKey: 'FOOTER.ABOUT_LINKS.HOW_IT_WORKS', path: '/about' },
    { labelKey: 'FOOTER.ABOUT_LINKS.OUR_TEAM',     path: '/about' },
    { labelKey: 'FOOTER.ABOUT_LINKS.OUR_CLIENTS',  path: '/about' },
  ];

  serviceLinks = [
    { labelKey: 'FOOTER.SERVICE_LINKS.VALUATION',   path: '/services' },
    { labelKey: 'FOOTER.SERVICE_LINKS.MARKETING',   path: '/services' },
    { labelKey: 'FOOTER.SERVICE_LINKS.NEGOTIATION', path: '/services' },
    { labelKey: 'FOOTER.SERVICE_LINKS.CLOSING',     path: '/services' },
    { labelKey: 'FOOTER.SERVICE_LINKS.MANAGEMENT',  path: '/services' },
  ];

  legalLinks = [
    { labelKey: 'FOOTER.LEGAL.PRIVACY',   path: '/privacy' },
    { labelKey: 'FOOTER.LEGAL.TERMS',     path: '/terms' },
    { labelKey: 'FOOTER.LEGAL.COOKIES',   path: '/cookies' },
    { labelKey: 'FOOTER.LEGAL.LOCATIONS', path: '/locations' },
  ];
}
