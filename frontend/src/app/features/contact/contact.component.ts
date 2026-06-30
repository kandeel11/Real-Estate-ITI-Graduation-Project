import { Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { InViewDirective } from '../../shared/directives/in-view.directive';

@Component({
  selector: 'app-contact',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, InViewDirective, TranslatePipe],
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

  // Label/sub hold i18n keys; value is literal data (emails, phones, place names).
  readonly contactInfo = [
    { icon: 'fa-envelope',     color: '#4f46e5', bg: 'rgba(79,70,229,.1)', label: 'CONTACT.INFO_EMAIL_LABEL', value: 'hello@estatein.io',     sub: 'CONTACT.INFO_EMAIL_SUB' },
    { icon: 'fa-phone',        color: '#059669', bg: 'rgba(5,150,105,.1)', label: 'CONTACT.INFO_PHONE_LABEL', value: '+20 123 456 7890',     sub: 'CONTACT.INFO_PHONE_SUB' },
    { icon: 'fa-location-dot', color: '#dc2626', bg: 'rgba(220,38,38,.1)', label: 'CONTACT.INFO_VISIT_LABEL', value: 'Smart Village, Cairo', sub: 'CONTACT.INFO_VISIT_SUB' },
    { icon: 'fa-clock',        color: '#d97706', bg: 'rgba(217,119,6,.1)', label: 'CONTACT.INFO_HOURS_LABEL', value: 'Sun – Thu',            sub: 'CONTACT.INFO_HOURS_SUB' },
  ];

  readonly subjects = [
    { value: 'General Inquiry',          labelKey: 'CONTACT.SUBJ_GENERAL' },
    { value: 'Property Listing',         labelKey: 'CONTACT.SUBJ_LISTING' },
    { value: 'Technical Support',        labelKey: 'CONTACT.SUBJ_SUPPORT' },
    { value: 'Subscription & Billing',   labelKey: 'CONTACT.SUBJ_BILLING' },
    { value: 'Partnership Opportunity',  labelKey: 'CONTACT.SUBJ_PARTNERSHIP' },
    { value: 'Other',                    labelKey: 'CONTACT.SUBJ_OTHER' },
  ];

  // Names/initials are not translated; role + bio resolve via the translate pipe.
  readonly team = [
    { name: 'Mohamed Khaled Kandel', role: 'CONTACT.TEAM1_ROLE', bio: 'CONTACT.TEAM1_BIO', gradient: 'linear-gradient(135deg,#3525cd,#6366f1)', initials: 'MK', social: { linkedin: '#', twitter: '#' } },
    { name: 'Ahmed Shipl',           role: 'CONTACT.TEAM2_ROLE', bio: 'CONTACT.TEAM2_BIO', gradient: 'linear-gradient(135deg,#0891b2,#38bdf8)', initials: 'AS', social: { linkedin: '#', twitter: '#' } },
    { name: 'Nawal Ali',             role: 'CONTACT.TEAM3_ROLE', bio: 'CONTACT.TEAM3_BIO', gradient: 'linear-gradient(135deg,#059669,#34d399)', initials: 'NA', social: { linkedin: '#', twitter: '#' } },
    { name: 'Yasser',                role: 'CONTACT.TEAM4_ROLE', bio: 'CONTACT.TEAM4_BIO', gradient: 'linear-gradient(135deg,#7c3aed,#a78bfa)', initials: 'YA', social: { linkedin: '#', twitter: '#' } },
    { name: 'Alyaa Shahin',          role: 'CONTACT.TEAM5_ROLE', bio: 'CONTACT.TEAM5_BIO', gradient: 'linear-gradient(135deg,#d97706,#fbbf24)', initials: 'AL', social: { linkedin: '#', twitter: '#' } },
    { name: 'Sara Samir',            role: 'CONTACT.TEAM6_ROLE', bio: 'CONTACT.TEAM6_BIO', gradient: 'linear-gradient(135deg,#db2777,#f472b6)', initials: 'SS', social: { linkedin: '#', twitter: '#' } },
  ];
}
