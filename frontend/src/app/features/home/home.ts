import { Component, inject, signal, computed, OnInit, OnDestroy, HostListener } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { InViewDirective } from '../../shared/directives/in-view.directive';
import { SearchService } from '../../features/ai/Search/Services/search-service';
import { ParsedQuery, PropertyResult } from '../../features/ai/Search/models';
import { PropertyService } from '../../features/properties/services/property.service';
import { Property } from '../../features/properties/models/property.model';
import { AgentAvatarComponent } from '../../shared/components/agent-avatar/agent-avatar.component';

type SearchMode = 'regular' | 'smart';

const MAX_FEATURED = 15;

@Component({
  selector: 'app-home',
  imports: [InViewDirective, RouterLink, TranslatePipe, AgentAvatarComponent],
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class HomeComponent implements OnInit, OnDestroy {
  private searchService = inject(SearchService);
  private propertyService = inject(PropertyService);
  private router = inject(Router);

  // ── Search mode ────────────────────────────────────────────
  searchMode = signal<SearchMode>('regular');
  locationQuery = signal('');   // structured location input
  query = signal('');           // AI full-text query
  loading = signal(false);
  hasSearched = signal(false);
  errorMsg = signal('');

  results = signal<PropertyResult[]>([]);
  totalResults = signal(0);
  parsedQuery = signal<ParsedQuery | null>(null);

  // ── Structured hero filters ────────────────────────────────
  selectedTypes = signal<Set<string>>(new Set());
  selectedBeds = signal<number | null>(null);
  homeMinPrice = signal<number | null>(null);
  homeMaxPrice = signal<number | null>(null);
  homeFurnished = signal(false);
  moreFiltersOpen = signal(false);

  readonly homeTypeOptions = ['Apartment', 'Villa', 'House'];
  readonly homeBedOptions = [1, 2, 3, 4, 5];

  // ── Static data ────────────────────────────────────────────
  services = [
    { icon: 'fa-solid fa-house',                   titleKey: 'HOME.SERVICES.FIND_HOME.TITLE',    descKey: 'HOME.SERVICES.FIND_HOME.DESC' },
    { icon: 'fa-solid fa-coins',                   titleKey: 'HOME.SERVICES.UNLOCK_VALUE.TITLE', descKey: 'HOME.SERVICES.UNLOCK_VALUE.DESC' },
    { icon: 'fa-solid fa-building',                titleKey: 'HOME.SERVICES.MANAGEMENT.TITLE',   descKey: 'HOME.SERVICES.MANAGEMENT.DESC' },
    { icon: 'fa-solid fa-chart-line',              titleKey: 'HOME.SERVICES.INVESTMENTS.TITLE',  descKey: 'HOME.SERVICES.INVESTMENTS.DESC' },
    { icon: 'fa-solid fa-magnifying-glass-dollar', titleKey: 'HOME.SERVICES.PRICING.TITLE',      descKey: 'HOME.SERVICES.PRICING.DESC' },
    { icon: 'fa-solid fa-comments',                titleKey: 'HOME.SERVICES.TIPS.TITLE',         descKey: 'HOME.SERVICES.TIPS.DESC' },
  ];

  topAreas = [
    { name: 'Cairo',      count: '2,480', image: 'https://images.unsplash.com/photo-1572252009286-268acec5ca0a?auto=format&fit=crop&w=600&q=80' },
    { name: 'Alexandria', count: '1,210', image: 'https://images.unsplash.com/photo-1697546889969-27f7b5be8664?auto=format&fit=crop&w=600&q=80' },
    { name: 'Giza',       count: '890',   image: 'https://images.unsplash.com/photo-1553913861-c0fddf2619ee?auto=format&fit=crop&w=600&q=80' },
    { name: 'New Cairo',  count: '640',   image: 'https://images.unsplash.com/photo-1568605114967-8130f3a36994?auto=format&fit=crop&w=600&q=80' },
    { name: 'Maadi',      count: '420',   image: 'https://images.unsplash.com/photo-1580587771525-78b9dba3b914?auto=format&fit=crop&w=600&q=80' },
    { name: 'Tanta',      count: '380',   image: 'https://images.unsplash.com/photo-1609128437842-bf1f8df85bee?auto=format&fit=crop&w=600&q=80' },
  ];

  // ── Featured properties carousel ───────────────────────────
  properties = signal<Property[]>([]);
  propertiesLoading = signal(true);

  slide = signal(0);
  cardsPerView = signal(3);
  private autoplayId: ReturnType<typeof setInterval> | null = null;

  maxSlide  = computed(() => Math.max(0, this.properties().length - this.cardsPerView()));
  trackOffset = computed(() => -(this.slide() * (100 / this.cardsPerView())));
  hasCarousel = computed(() => this.properties().length > this.cardsPerView());
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

  ngOnDestroy(): void { this.stopAutoplay(); }

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
      error: () => { this.properties.set([]); this.propertiesLoading.set(false); },
    });
  }

  // ── Carousel controls ───────────────────────────────────────
  nextSlide(): void { this.slide.set(this.slide() >= this.maxSlide() ? 0 : this.slide() + 1); }
  prevSlide(): void { this.slide.set(this.slide() <= 0 ? this.maxSlide() : this.slide() - 1); }
  goToSlide(i: number): void { this.slide.set(Math.min(Math.max(i, 0), this.maxSlide())); }
  pauseAutoplay(): void  { this.stopAutoplay(); }
  resumeAutoplay(): void { this.startAutoplay(); }

  private startAutoplay(): void {
    this.stopAutoplay();
    if (!this.hasCarousel()) return;
    this.autoplayId = setInterval(() => this.nextSlide(), 4000);
  }
  private stopAutoplay(): void {
    if (this.autoplayId !== null) { clearInterval(this.autoplayId); this.autoplayId = null; }
  }

  @HostListener('window:resize')
  updateCardsPerView(): void {
    const w = window.innerWidth;
    const next = w < 768 ? 1 : w < 992 ? 2 : 3;
    if (next !== this.cardsPerView()) {
      this.cardsPerView.set(next);
      if (this.slide() > this.maxSlide()) this.slide.set(this.maxSlide());
    }
  }

  typeIcon(type: string): string {
    switch (type) {
      case 'Villa': return 'fa-solid fa-house-chimney';
      case 'House': return 'fa-solid fa-house-user';
      default:      return 'fa-solid fa-building';
    }
  }

  openProperty(id: string): void { this.router.navigate(['/properties', id]); }
  goToProperty(p: PropertyResult): void { this.router.navigate(['/properties', p.propertyId]); }

  // ── Structured filter helpers ───────────────────────────────
  toggleHomeType(t: string): void {
    const next = new Set(this.selectedTypes());
    next.has(t) ? next.delete(t) : next.add(t);
    this.selectedTypes.set(next);
  }

  setHomeMin(e: Event): void {
    const v = (e.target as HTMLInputElement).value;
    this.homeMinPrice.set(v ? Number(v) : null);
  }

  setHomeMax(e: Event): void {
    const v = (e.target as HTMLInputElement).value;
    this.homeMaxPrice.set(v ? Number(v) : null);
  }

  /** Navigate to /properties with the selected home-page filters applied as query params. */
  navigateToBrowse(city?: string): void {
    const params: Record<string, string> = {};
    const loc = city ?? this.locationQuery().trim();
    if (loc)                            params['search']    = loc;
    if (this.selectedTypes().size)      params['types']     = [...this.selectedTypes()].join(',');
    if (this.selectedBeds() !== null)   params['beds']      = String(this.selectedBeds());
    if (this.homeMinPrice() !== null)   params['minPrice']  = String(this.homeMinPrice());
    if (this.homeMaxPrice() !== null)   params['maxPrice']  = String(this.homeMaxPrice());
    if (this.homeFurnished())           params['furnished'] = 'true';
    this.router.navigate(['/properties'], { queryParams: params });
  }

  // ── AI / smart search ───────────────────────────────────────
  toggleMode(): void {
    this.searchMode.set(this.searchMode() === 'regular' ? 'smart' : 'regular');
    this.clearResults();
  }

  clearResults(): void {
    this.results.set([]); this.parsedQuery.set(null);
    this.hasSearched.set(false); this.totalResults.set(0); this.errorMsg.set('');
  }

  onSubmit(): void {
    if (this.searchMode() === 'regular') { this.navigateToBrowse(); return; }

    const q = this.query().trim();
    if (!q || this.loading()) return;
    this.loading.set(true);
    this.errorMsg.set('');
    this.parsedQuery.set(null);
    this.runSmartSearch(q);
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

  getParsedChips(): { label: string; value: string }[] {
    const p = this.parsedQuery();
    if (!p) return [];
    const chips: { label: string; value: string }[] = [];
    if (p.propertyType)    chips.push({ label: 'Type',   value: p.propertyType });
    if (p.city)            chips.push({ label: 'City',   value: p.city });
    if (p.district)        chips.push({ label: 'Area',   value: p.district });
    if (p.nearbyLandmark)  chips.push({ label: 'Near',   value: p.nearbyLandmark });
    if (p.bedrooms)        chips.push({ label: 'Beds',   value: `${p.bedrooms}` });
    if (p.bathrooms)       chips.push({ label: 'Baths',  value: `${p.bathrooms}` });
    if (p.minArea || p.maxArea)   chips.push({ label: 'Area',  value: `${p.minArea ?? 0}–${p.maxArea ?? '∞'} m²` });
    if (p.minPrice || p.maxPrice) chips.push({ label: 'Price', value: `${p.minPrice ?? 0}–${p.maxPrice ?? '∞'}` });
    if (p.amenities?.length)      chips.push({ label: 'Amenities', value: p.amenities.join(', ') });
    return chips;
  }

  formatPrice(price: number): string {
    if (!price) return '—';
    if (price >= 1_000_000) return `$${(price / 1_000_000).toFixed(1)}M`;
    if (price >= 1_000)     return `$${(price / 1_000).toFixed(0)}K`;
    return `$${price}`;
  }

  matchPercent(score: number): number { return Math.round((score ?? 0) * 100); }
}
