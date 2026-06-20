import { Component, inject, signal, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { InViewDirective } from '../../shared/directives/in-view.directive';
import { SearchService } from '../../features/ai/Search/Services/search-service';
import { ParsedQuery, PropertyResult } from '../../features/ai/Search/models';
import { PropertyService } from '../../features/properties/services/property.service';
import { Property } from '../../features/properties/models/property.model';

type SearchMode = 'regular' | 'smart';

@Component({
  selector: 'app-home',
  imports: [InViewDirective, RouterLink, TranslatePipe],
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class HomeComponent implements OnInit {
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

  /** Featured listings pulled from the public active feed. */
  properties = signal<Property[]>([]);
  propertiesLoading = signal(true);

  faqs = [
    { qKey: 'HOME.FAQ.Q1', aKey: 'HOME.FAQ.A1' },
    { qKey: 'HOME.FAQ.Q2', aKey: 'HOME.FAQ.A2' },
    { qKey: 'HOME.FAQ.Q3', aKey: 'HOME.FAQ.A3' },
    { qKey: 'HOME.FAQ.Q4', aKey: 'HOME.FAQ.A4' },
    { qKey: 'HOME.FAQ.Q5', aKey: 'HOME.FAQ.A5' },
    { qKey: 'HOME.FAQ.Q6', aKey: 'HOME.FAQ.A6' },
  ];

  ngOnInit(): void {
    this.loadFeatured();
  }

  /**
   * Load featured listings for the home page. Prefers properties flagged
   * `isFeatured`; falls back to the most recent active listings so the
   * section is never empty.
   */
  private loadFeatured(): void {
    this.propertiesLoading.set(true);
    this.propertyService.getActive().subscribe({
      next: (list) => {
        const featured = list.filter((p) => p.isFeatured);
        const pool = featured.length ? featured : list;
        this.properties.set(pool.slice(0, 3));
        this.propertiesLoading.set(false);
      },
      error: () => {
        this.properties.set([]);
        this.propertiesLoading.set(false);
      },
    });
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
