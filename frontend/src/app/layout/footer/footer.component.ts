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

  navLinks = [
    { label: 'Home',        path: '/',           params: null },
    { label: 'Properties',  path: '/properties', params: null },
    { label: 'Services',    path: '/services',   params: null },
    { label: 'Contact',     path: '/contact',    params: null },
  ];

  propertyLinks = [
    { label: 'Browse All',   path: '/properties', params: null },
    { label: 'Featured',     path: '/properties', params: { featured: 'true' } },
    { label: 'Apartments',   path: '/properties', params: { types: 'Apartment' } },
    { label: 'Villas',       path: '/properties', params: { types: 'Villa' } },
    { label: 'Houses',       path: '/properties', params: { types: 'House' } },
  ];

  contactInfo = [
    { icon: 'fa-solid fa-envelope',     text: 'info@estatein.com',    href: 'mailto:info@estatein.com' },
    { icon: 'fa-solid fa-phone',        text: '+20 100 000 0000',     href: 'tel:+201000000000' },
    { icon: 'fa-solid fa-location-dot', text: 'Cairo, Egypt',         href: null },
    { icon: 'fa-regular fa-clock',      text: 'Mon – Sat  9 AM – 6 PM', href: null },
  ];

  socials = [
    { icon: 'fa-brands fa-facebook-f',  label: 'Facebook',   href: '#' },
    { icon: 'fa-brands fa-x-twitter',   label: 'Twitter/X',  href: '#' },
    { icon: 'fa-brands fa-instagram',   label: 'Instagram',  href: '#' },
    { icon: 'fa-brands fa-linkedin-in', label: 'LinkedIn',   href: '#' },
    { icon: 'fa-brands fa-whatsapp',    label: 'WhatsApp',   href: '#' },
  ];

  legalLinks = [
    { label: 'Privacy Policy',   path: '/privacy' },
    { label: 'Terms of Service', path: '/terms' },
    { label: 'Cookie Policy',    path: '/cookies' },
  ];
}
