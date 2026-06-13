import { Component, signal, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

type SettingsTab = 'profile' | 'platform' | 'notifications' | 'security' | 'appearance';
type Toast = { type: 'success' | 'error'; text: string } | null;

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SettingsComponent {
  activeTab = signal<SettingsTab>('profile');
  saving = signal(false);
  toast = signal<Toast>(null);

  // ===== Profile =====
  profile = signal({
    fullName: 'Platform Admin',
    email: 'admin@realestate.com',
    phone: '+201000000000',
    role: 'Administrator',
    bio: 'Managing the real estate platform since 2024.',
    avatar: '',
  });

  // ===== Platform =====
  platform = signal({
    platformName: 'RealEstate Egypt',
    supportEmail: 'support@realestate.com',
    timezone: 'Africa/Cairo',
    currency: 'EGP',
    language: 'en',
    maxListingsPerUser: 20,
    maintenanceMode: false,
    autoApproveListings: false,
    requireEmailVerify: true,
    allowGuestSearch: true,
  });

  // ===== Notifications =====
  notifications = signal({
    emailNewListing: true,
    emailNewUser: true,
    emailNewReport: false,
    emailWeeklyDigest: true,
    pushNewInquiry: true,
    pushVerificationDone: true,
    pushSystemAlerts: true,
    smsOTP: true,
  });

  // ===== Security =====
  security = signal({
    currentPassword: '',
    newPassword: '',
    confirmPassword: '',
    twoFAEnabled: false,
    sessionTimeout: 60,
    ipWhitelist: '',
  });

  // ===== Appearance =====
  appearance = signal({
    primaryColor: '#4f46e5',
    accentColor: '#10b981',
    sidebarStyle: 'dark',
    compactMode: false,
    showBreadcrumbs: true,
    animationsOn: true,
    logoUrl: '',
  });

  tabs: { key: SettingsTab; label: string; icon: string }[] = [
    { key: 'profile', label: 'Profile', icon: 'fa-user-circle' },
    { key: 'platform', label: 'Platform', icon: 'fa-sliders' },
    { key: 'notifications', label: 'Notifications', icon: 'fa-bell' },
    { key: 'security', label: 'Security', icon: 'fa-shield-halved' },
    { key: 'appearance', label: 'Appearance', icon: 'fa-palette' },
  ];

  timezones = ['Africa/Cairo', 'UTC', 'Europe/London', 'America/New_York', 'Asia/Dubai'];
  currencies = ['EGP', 'USD', 'EUR', 'AED'];
  languages = [
    { k: 'en', l: 'English' },
    { k: 'ar', l: 'العربية' },
  ];
  sessionTimeouts = [15, 30, 60, 120, 240];

  setTab(t: SettingsTab) {
    this.activeTab.set(t);
  }

  save() {
    this.saving.set(true);
    setTimeout(() => {
      this.saving.set(false);
      this.showToast('success', 'Settings saved successfully!');
    }, 1200);
  }

  updateProfile(key: string, val: string) {
    this.profile.update((p) => ({ ...p, [key]: val }));
  }
  updatePlatform(key: string, val: any) {
    this.platform.update((p) => ({ ...p, [key]: val }));
  }
  updateNotif(key: string, val: boolean) {
    this.notifications.update((n) => ({ ...n, [key]: val }));
  }
  updateSecurity(key: string, val: any) {
    this.security.update((s) => ({ ...s, [key]: val }));
  }
  updateAppearance(key: string, val: any) {
    this.appearance.update((a) => ({ ...a, [key]: val }));
  }

  passwordsMatch = () => {
    const s = this.security();
    return s.newPassword.length >= 8 && s.newPassword === s.confirmPassword;
  };

  showToast(type: 'success' | 'error', text: string) {
    this.toast.set({ type, text });
    setTimeout(() => this.toast.set(null), 3000);
  }
  getInitials() {
    return this.profile()
      .fullName.split(' ')
      .map((n) => n[0])
      .join('')
      .slice(0, 2)
      .toUpperCase();
  }
}
