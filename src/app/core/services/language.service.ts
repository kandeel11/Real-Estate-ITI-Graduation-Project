import { Injectable, signal, computed, inject, effect } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

export type Lang = 'en' | 'ar';

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly translate = inject(TranslateService);

  readonly currentLang = signal<Lang>((localStorage.getItem('lang') as Lang) ?? 'en');
  readonly isRtl = computed(() => this.currentLang() === 'ar');

  constructor() {
    effect(() => this.applyLang(this.currentLang()));
  }

  toggle(): void {
    this.currentLang.set(this.currentLang() === 'en' ? 'ar' : 'en');
  }

  private applyLang(lang: Lang): void {
    this.translate.use(lang);
    localStorage.setItem('lang', lang);
    document.documentElement.setAttribute('lang', lang);
    document.documentElement.setAttribute('dir', lang === 'ar' ? 'rtl' : 'ltr');

    const ltr = document.getElementById('bs-ltr') as HTMLLinkElement | null;
    const rtl = document.getElementById('bs-rtl') as HTMLLinkElement | null;
    if (ltr) ltr.disabled = lang === 'ar';
    if (rtl) rtl.disabled = lang === 'en';

    document.body.classList.toggle('lang-ar', lang === 'ar');
    document.body.classList.toggle('lang-en', lang === 'en');
  }
}
