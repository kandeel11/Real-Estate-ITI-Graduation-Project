import { Component, inject, signal, computed, OnInit, OnDestroy, HostListener } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { InViewDirective } from '../../shared/directives/in-view.directive';
import { SearchService } from '../../features/ai/Search/Services/search-service';
import { ParsedQuery, PropertyResult } from '../../features/ai/Search/models';
import { PropertyService } from '../../features/properties/services/property.service';
import { Property } from '../../features/properties/models/property.model';

type SearchMode = 'regular' | 'smart';

/** Cap on how many featured listings the home carousel will cycle through. */
const MAX_FEATURED = 15;

@Component({
  selector: 'app-home',
  imports: [InViewDirective, RouterLink, TranslatePipe],
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class HomeComponent implements OnInit, OnDestroy {
  // ============================================================
  // 🔎 SEARCH STATE (NEW)
  // ============================================================
  private searchService = inject(SearchService);
  private propertyService = inject(PropertyService);
  private router = inject(Router);

  searchMode = signal<SearchMode>('regular');
  query = signal('');
  loading = signal(false);
  hasSearched = signal(false);
  errorMsg = signal('');

  results = signal<PropertyResult[]>([]);
  totalResults = signal(0);
  parsedQuery = signal<ParsedQuery | null>(null);

  // ============================================================
  // 🎯 STATIC DATA (KEEP YOUR EXISTING)
  // ============================================================
  services = [
    { icon: 'fa-solid fa-house',       titleKey: 'HOME.SERVICES.FIND_HOME.TITLE',    descKey: 'HOME.SERVICES.FIND_HOME.DESC' },
    { icon: 'fa-solid fa-coins',       titleKey: 'HOME.SERVICES.UNLOCK_VALUE.TITLE', descKey: 'HOME.SERVICES.UNLOCK_VALUE.DESC' },
    { icon: 'fa-solid fa-building',    titleKey: 'HOME.SERVICES.MANAGEMENT.TITLE',   descKey: 'HOME.SERVICES.MANAGEMENT.DESC' },
    { icon: 'fa-solid fa-chart-line',  titleKey: 'HOME.SERVICES.INVESTMENTS.TITLE',  descKey: 'HOME.SERVICES.INVESTMENTS.DESC' },
  ];

  /** Featured listings pulled from the public active feed (up to {@link MAX_FEATURED}). */
  properties = signal<Property[]>([]);
  propertiesLoading = signal(true);

  // ── Featured carousel state ─────────────────────────────────────
  /** Index of the left-most visible card. */
  slide = signal(0);
  /** Cards visible at once — responsive (1 mobile / 2 tablet / 3 desktop). */
  cardsPerView = signal(3);
  private autoplayId: ReturnType<typeof setInterval> | null = null;

  /** Highest slide index that still fills the viewport. */
  maxSlide = computed(() => Math.max(0, this.properties().length - this.cardsPerView()));

  /** How far the track is shifted, as a percentage of one viewport width. */
  trackOffset = computed(() => -(this.slide() * (100 / this.cardsPerView())));

  /** Whether the carousel has more cards than fit on screen. */
  hasCarousel = computed(() => this.properties().length > this.cardsPerView());

  /** One dot per slide position. */
  slideDots = computed(() => Array.from({ length: this.maxSlide() + 1 }, (_, i) => i));

  faqs = [
    { qKey: 'HOME.FAQ.Q1', aKey: 'HOME.FAQ.A1' },
    { qKey: 'HOME.FAQ.Q2', aKey: 'HOME.FAQ.A2' },
    { qKey: 'HOME.FAQ.Q3', aKey: 'HOME.FAQ.A3' },
    { qKey: 'HOME.FAQ.Q4', aKey: 'HOME.FAQ.A4' },
    { qKey: 'HOME.FAQ.Q5', aKey: 'HOME.FAQ.A5' },
    { qKey: 'HOME.FAQ.Q6', aKey: 'HOME.FAQ.A6' },
  ];

  ngOnInit(): void {
    this.updateCardsPerView();
    this.loadFeatured();
  }

  ngOnDestroy(): void {
    this.stopAutoplay();
  }

  /**
   * Load featured listings for the home page. Prefers properties flagged
   * `isFeatured`; falls back to the most recent active listings so the
   * section is never empty. Caps the carousel at {@link MAX_FEATURED}.
   */
  private loadFeatured(): void {
    this.propertiesLoading.set(true);
    this.propertyService.getActive().subscribe({
      next: (list) => {
        const featured = list.filter((p) => p.isFeatured);
        const pool = featured.length ? featured : list;
        this.properties.set(pool.slice(0, MAX_FEATURED));
        this.propertiesLoading.set(false);
        this.startAutoplay();
      },
      error: () => {
        this.properties.set([]);
        this.propertiesLoading.set(false);
      },
    });
  }

  // ── Carousel controls ───────────────────────────────────────────

  /** Advance one slide, wrapping back to the start at the end. */
  nextSlide(): void {
    this.slide.set(this.slide() >= this.maxSlide() ? 0 : this.slide() + 1);
  }

  /** Go back one slide, wrapping to the end at the start. */
  prevSlide(): void {
    this.slide.set(this.slide() <= 0 ? this.maxSlide() : this.slide() - 1);
  }

  goToSlide(i: number): void {
    this.slide.set(Math.min(Math.max(i, 0), this.maxSlide()));
  }

  /** Pause autoplay while the user hovers/interacts. */
  pauseAutoplay(): void { this.stopAutoplay(); }
  resumeAutoplay(): void { this.startAutoplay(); }

  private startAutoplay(): void {
    this.stopAutoplay();
    if (!this.hasCarousel()) return;
    this.autoplayId = setInterval(() => this.nextSlide(), 4000);
  }

  private stopAutoplay(): void {
    if (this.autoplayId !== null) {
      clearInterval(this.autoplayId);
      this.autoplayId = null;
    }
  }

  /** Keep cards-per-view in step with the viewport, clamping the slide index. */
  @HostListener('window:resize')
  updateCardsPerView(): void {
    const w = window.innerWidth;
    const next = w < 768 ? 1 : w < 992 ? 2 : 3;
    if (next !== this.cardsPerView()) {
      this.cardsPerView.set(next);
      if (this.slide() > this.maxSlide()) this.slide.set(this.maxSlide());
    }
  }

  /** Font Awesome icon for a property type. */
  typeIcon(type: string): string {
    switch (type) {
      case 'Villa': return 'fa-solid fa-house-chimney';
      case 'House': return 'fa-solid fa-house-user';
      default: return 'fa-solid fa-building';
    }
  }

  /** Open a featured property's detail page. */
  openProperty(id: string): void {
    this.router.navigate(['/properties', id]);
  }

  // ============================================================
  // 🔎 SEARCH METHODS (NEW)
  // ============================================================

  /** Toggle between regular and smart (AI) search mode */
  toggleMode(): void {
    this.searchMode.set(this.searchMode() === 'regular' ? 'smart' : 'regular');
    this.clearResults();
  }

  /** Clear current results and start fresh */
  clearResults(): void {
    this.results.set([]);
    this.parsedQuery.set(null);
    this.hasSearched.set(false);
    this.totalResults.set(0);
    this.errorMsg.set('');
  }

  /** Triggered when user submits the search */
  onSubmit(): void {
    const q = this.query().trim();
    if (!q || this.loading()) return;

    this.loading.set(true);
    this.errorMsg.set('');
    this.parsedQuery.set(null);

    if (this.searchMode() === 'smart') {
      this.runSmartSearch(q);
    } else {
      this.runRegularSearch(q);
    }
  }

  private runSmartSearch(q: string): void {
    this.searchService.smartSearch({ query: q, page: 1, pageSize: 5 }).subscribe({
      next: (res) => {
        this.loading.set(false);
        this.hasSearched.set(true);
        this.parsedQuery.set(res.parsedQuery);
        this.results.set(res.properties ?? []);
        this.totalResults.set(res.totalResults ?? 0);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMsg.set(err.message || 'AI search failed.');
        this.results.set([]);
      },
    });
  }

  private runRegularSearch(q: string): void {
    // مؤقتاً نستخدم smart endpoint كـ fallback
    // غيّرها لما تجهز endpoint للـ regular search في الباك
    this.searchService.smartSearch({ query: q, page: 1, pageSize: 10 }).subscribe({
      next: (res) => {
        this.loading.set(false);
        this.hasSearched.set(true);
        this.results.set(res.properties ?? []);
        this.totalResults.set(res.totalResults ?? 0);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMsg.set(err.message || 'Search failed.');
        this.results.set([]);
      },
    });
  }

  /** Build the AI-detected chips array for the UI */
  getParsedChips(): { label: string; value: string }[] {
    const p = this.parsedQuery();
    if (!p) return [];
    const chips: { label: string; value: string }[] = [];

    if (p.propertyType) chips.push({ label: 'Type', value: p.propertyType });
    if (p.city) chips.push({ label: 'City', value: p.city });
    if (p.district) chips.push({ label: 'District', value: p.district });
    if (p.nearbyLandmark) chips.push({ label: 'Near', value: p.nearbyLandmark });
    if (p.bedrooms) chips.push({ label: 'Beds', value: `${p.bedrooms}` });
    if (p.bathrooms) chips.push({ label: 'Baths', value: `${p.bathrooms}` });
    if (p.floor) chips.push({ label: 'Floor', value: `${p.floor}` });
    if (p.minArea || p.maxArea) {
      chips.push({
        label: 'Area',
        value: `${p.minArea ?? 0} - ${p.maxArea ?? '∞'} m²`,
      });
    }
    if (p.minPrice || p.maxPrice) {
      chips.push({
        label: 'Price',
        value: `${p.minPrice ? '$' + p.minPrice : '0'} - ${p.maxPrice ? '$' + p.maxPrice : '∞'}`,
      });
    }
    if (p.amenities?.length) {
      chips.push({ label: 'Amenities', value: p.amenities.join(', ') });
    }
    return chips;
  }

  /** Navigate to property details */
  goToProperty(p: PropertyResult): void {
    this.router.navigate(['/properties', p.propertyId]);
  }

  /** Pretty-print prices ($550K / $1.2M) */
  formatPrice(price: number): string {
    if (!price) return '—';
    if (price >= 1_000_000) return `$${(price / 1_000_000).toFixed(1)}M`;
    if (price >= 1_000) return `$${(price / 1_000).toFixed(0)}K`;
    return `$${price}`;
  }

  /** Convert matchScore (0–1) to a 0–100% display */
  matchPercent(score: number): number {
    return Math.round((score ?? 0) * 100);
  }
}
