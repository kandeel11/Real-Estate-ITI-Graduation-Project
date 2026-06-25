import {
  AfterViewInit,
  Component,
  ElementRef,
  EventEmitter,
  HostListener,
  Output,
  ViewChild,
  inject,
  signal,
} from '@angular/core';
import { Router } from '@angular/router';
import { SearchService } from '../../../features/ai/Search/Services/search-service';
import { ParsedQuery, PropertyResult } from '../../../features/ai/Search/models';

type SearchMode = 'regular' | 'smart';

@Component({
  selector: 'app-search-overlay',
  standalone: true,
  imports: [],
  templateUrl: './search-overlay.component.html',
  styleUrl: './search-overlay.component.scss',
})
export class SearchOverlayComponent implements AfterViewInit {
  @Output() closed = new EventEmitter<void>();
  @ViewChild('searchInput') searchInputRef!: ElementRef<HTMLInputElement>;

  private readonly searchService = inject(SearchService);
  private readonly router = inject(Router);

  searchMode = signal<SearchMode>('regular');
  query = signal('');
  loading = signal(false);
  hasSearched = signal(false);
  errorMsg = signal('');
  results = signal<PropertyResult[]>([]);
  totalResults = signal(0);
  parsedQuery = signal<ParsedQuery | null>(null);

  /** Active image index per card, keyed by propertyId. */
  protected activeImgIdx = signal<Record<string, number>>({});

  protected getActiveImg(p: PropertyResult): string | null {
    const idx = this.activeImgIdx()[p.propertyId] ?? 0;
    if (p.imageUrls?.length) return p.imageUrls[idx] ?? p.mainImageUrl;
    return p.mainImageUrl;
  }

  protected getActiveIdx(p: PropertyResult): number {
    return this.activeImgIdx()[p.propertyId] ?? 0;
  }

  protected stepImg(p: PropertyResult, dir: 1 | -1, e: Event): void {
    e.stopPropagation();
    const urls = p.imageUrls;
    if (!urls?.length || urls.length <= 1) return;
    const cur = this.getActiveIdx(p);
    this.activeImgIdx.update(m => ({ ...m, [p.propertyId]: (cur + dir + urls.length) % urls.length }));
  }

  protected jumpImg(p: PropertyResult, idx: number, e: Event): void {
    e.stopPropagation();
    this.activeImgIdx.update(m => ({ ...m, [p.propertyId]: idx }));
  }

  readonly suggestions = [
    'Villa with pool in New Cairo',
    '2 bedroom apartment near Metro',
    'Studio for rent under $500/month',
    'Office space in 6th of October',
  ];

  ngAfterViewInit(): void {
    setTimeout(() => this.searchInputRef?.nativeElement.focus(), 80);
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.closed.emit();
  }

  close(): void {
    this.closed.emit();
  }

  toggleMode(): void {
    this.searchMode.set(this.searchMode() === 'regular' ? 'smart' : 'regular');
    this.clearResults();
    this.searchInputRef?.nativeElement.focus();
  }

  clearResults(): void {
    this.results.set([]);
    this.parsedQuery.set(null);
    this.hasSearched.set(false);
    this.totalResults.set(0);
    this.errorMsg.set('');
  }

  useSuggestion(text: string): void {
    this.query.set(text);
    if (this.searchInputRef) this.searchInputRef.nativeElement.value = text;
    this.searchMode.set('smart');
    this.onSubmit();
  }

  onSubmit(): void {
    const q = this.query().trim();
    if (!q || this.loading()) return;
    this.loading.set(true);
    this.errorMsg.set('');
    this.parsedQuery.set(null);

    const pageSize = this.searchMode() === 'smart' ? 6 : 10;
    this.searchService.smartSearch({ query: q, page: 1, pageSize }).subscribe({
      next: (res) => {
        this.loading.set(false);
        this.hasSearched.set(true);
        if (this.searchMode() === 'smart') this.parsedQuery.set(res.parsedQuery);
        this.results.set(res.properties ?? []);
        this.totalResults.set(res.totalResults ?? 0);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMsg.set(err.message || 'Search failed. Please try again.');
        this.results.set([]);
      },
    });
  }

  goToProperty(p: PropertyResult): void {
    this.router.navigate(['/properties', p.propertyId]);
    this.closed.emit();
  }

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
    if (p.minArea || p.maxArea)
      chips.push({ label: 'Area', value: `${p.minArea ?? 0} – ${p.maxArea ?? '∞'} m²` });
    if (p.minPrice || p.maxPrice)
      chips.push({
        label: 'Price',
        value: `${p.minPrice ? '$' + p.minPrice : '0'} – ${p.maxPrice ? '$' + p.maxPrice : '∞'}`,
      });
    if (p.amenities?.length) chips.push({ label: 'Amenities', value: p.amenities.join(', ') });
    return chips;
  }

  formatPrice(price: number): string {
    if (!price) return '—';
    if (price >= 1_000_000) return `$${(price / 1_000_000).toFixed(1)}M`;
    if (price >= 1_000) return `$${(price / 1_000).toFixed(0)}K`;
    return `$${price}`;
  }
}
