import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-footer',
  imports: [RouterLink],
  templateUrl: './footer.component.html',
  styleUrl: './footer.component.css'
})
export class FooterComponent {
  currentYear = new Date().getFullYear();

  homeLinks = [
    { label: 'Hero Section', path: '/' },
    { label: 'Features',     path: '/' },
    { label: 'Properties',   path: '/properties' },
    { label: 'Testimonials', path: '/' },
    { label: "FAQ's",        path: '/' },
  ];

  aboutLinks = [
    { label: 'Our Story',    path: '/about' },
    { label: 'Our Works',    path: '/about' },
    { label: 'How It Works', path: '/about' },
    { label: 'Our Team',     path: '/about' },
    { label: 'Our Clients',  path: '/about' },
  ];

  serviceLinks = [
    { label: 'Valuation Mastery',    path: '/services' },
    { label: 'Strategic Marketing',  path: '/services' },
    { label: 'Negotiation Wizardry', path: '/services' },
    { label: 'Closing Success',      path: '/services' },
    { label: 'Property Management',  path: '/services' },
  ];

  legalLinks = [
    { label: 'Privacy Policy',   path: '/privacy' },
    { label: 'Terms of Service', path: '/terms' },
    { label: 'Cookie Settings',  path: '/cookies' },
    { label: 'Global Locations', path: '/locations' },
  ];
}
