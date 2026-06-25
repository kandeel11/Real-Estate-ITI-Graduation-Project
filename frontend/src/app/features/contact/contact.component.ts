import { Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { InViewDirective } from '../../shared/directives/in-view.directive';

@Component({
  selector: 'app-contact',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, InViewDirective],
  templateUrl: './contact.component.html',
  styleUrl: './contact.component.scss',
})
export class ContactComponent {
  private readonly fb = inject(FormBuilder);

  form = this.fb.group({
    fullName: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]],
    phone: [''],
    subject: ['', Validators.required],
    message: ['', [Validators.required, Validators.minLength(20)]],
  });

  sending = signal(false);
  sent = signal(false);
  errorMsg = signal<string | null>(null);

  get fullName() {
    return this.form.get('fullName')!;
  }
  get email() {
    return this.form.get('email')!;
  }
  get subject() {
    return this.form.get('subject')!;
  }
  get message() {
    return this.form.get('message')!;
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.sending.set(true);
    this.errorMsg.set(null);

    // Simulate sending — replace with real HttpClient call to your backend
    setTimeout(() => {
      this.sending.set(false);
      this.sent.set(true);
      this.form.reset();
    }, 1400);
  }

  resetForm(): void {
    this.sent.set(false);
    this.form.reset();
  }

  readonly contactInfo = [
    {
      icon: 'fa-envelope',
      color: '#4f46e5',
      bg: 'rgba(79,70,229,.1)',
      label: 'Email Us',
      value: 'hello@estatein.io',
      sub: 'We reply within 24 hours',
    },
    {
      icon: 'fa-phone',
      color: '#059669',
      bg: 'rgba(5,150,105,.1)',
      label: 'Call Us',
      value: '+20 123 456 7890',
      sub: 'Sun – Thu, 9am – 6pm EET',
    },
    {
      icon: 'fa-location-dot',
      color: '#dc2626',
      bg: 'rgba(220,38,38,.1)',
      label: 'Visit Us',
      value: 'Smart Village, Cairo',
      sub: 'Building B3, 6th Floor',
    },
    {
      icon: 'fa-clock',
      color: '#d97706',
      bg: 'rgba(217,119,6,.1)',
      label: 'Working Hours',
      value: 'Sun – Thu',
      sub: '9:00 AM – 6:00 PM EET',
    },
  ];

  readonly subjects = [
    'General Inquiry',
    'Property Listing',
    'Technical Support',
    'Subscription & Billing',
    'Partnership Opportunity',
    'Other',
  ];

  readonly team = [
    {
      name: 'Mohamed Khaled Kandel',
      role: 'AI Engineer',
      bio: 'Builds the AI-powered smart search and recommendation engine that drives intelligent property discovery across the platform.',
      gradient: 'linear-gradient(135deg,#3525cd,#6366f1)',
      initials: 'MK',
      social: { linkedin: '#', twitter: '#' },
    },
    {
      name: 'Ahmed Shipl',
      role: 'Team Leader & Property Integration',
      bio: 'Team lead responsible for the full property module — listing, details, integrations — and platform-wide subscription management.',
      gradient: 'linear-gradient(135deg,#0891b2,#38bdf8)',
      initials: 'AS',
      social: { linkedin: '#', twitter: '#' },
    },
    {
      name: 'Nawal Ali',
      role: 'Maintenance, Home & Tenant Dashboard',
      bio: 'Owns the home experience, tenant dashboard, and the full maintenance ticketing workflow from request to resolution.',
      gradient: 'linear-gradient(135deg,#059669,#34d399)',
      initials: 'NA',
      social: { linkedin: '#', twitter: '#' },
    },
    {
      name: 'Yasser',
      role: 'Broker Dashboard, Leases & Communication',
      bio: 'Leads the broker portal, lease management lifecycle, and in-platform communication features for seamless deal flow.',
      gradient: 'linear-gradient(135deg,#7c3aed,#a78bfa)',
      initials: 'YA',
      social: { linkedin: '#', twitter: '#' },
    },
    {
      name: 'Alyaa Shahin',
      role: 'Payment Systems',
      bio: 'Designs and implements the secure payment gateway, rent collection flows, and financial transaction management.',
      gradient: 'linear-gradient(135deg,#d97706,#fbbf24)',
      initials: 'AL',
      social: { linkedin: '#', twitter: '#' },
    },
    {
      name: 'Sara Samir',
      role: 'Identity & Authentication',
      bio: 'Builds and secures the authentication system — registration, OTP verification, role-based access control, and user identity.',
      gradient: 'linear-gradient(135deg,#db2777,#f472b6)',
      initials: 'SS',
      social: { linkedin: '#', twitter: '#' },
    },
  ];
}
