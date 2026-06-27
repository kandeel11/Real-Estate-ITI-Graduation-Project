/**
 * Public Property Browse
 * Sticky left filter sidebar + paginated results grid. Multi-select filters
 * (types, cities, bedroom counts, amenities) surface as dismissible chips;
 * price is a min/max range. All filtering is client-side over /properties/active.
 */

import { Component, HostListener, inject, signal, computed, effect, OnInit } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { PropertyService } from '../../services/property.service';
import { Property, Amenity } from '../../models/property.model';
import { SavedPropertyService } from '../../../../core/services/saved-property.service';
import { AuthService } from '../../../../core/services/auth.service';

const PAGE_SIZE = 12;

interface Chip {
  kind: 'type' | 'city' | 'beds' | 'amenity' | 'price' | 'furnished' | 'pets' | 'featured';
  value: string;
  label: string;
}

@Component({
  selector: 'app-property-browse',
  standalone: true,
  imports: [DecimalPipe, RouterLink, TranslatePipe],
  templateUrl: './browse.component.html',
  styleUrl: './browse.component.css',
})
export class BrowseComponent implements OnInit {
  private propertySvc = inject(PropertyService);
  protected savedSvc = inject(SavedPropertyService);
  private auth = inject(AuthService);
  private route = inject(ActivatedRoute);

  /** Only tenants get the wishlist toggle. */
  isTenant = computed(() => this.auth.userRole() === 'tenant');

  all = signal<Property[]>([]);
  amenities = signal<Amenity[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);

  // ── Filters (multi-select sets + range + booleans) ──────────────
  search = signal('');
  types = signal<Set<string>>(new Set());
  cities = signal<Set<string>>(new Set());
  beds = signal<Set<number>>(new Set());
  amenityFilter = signal<Set<string>>(new Set());
  minPrice = signal<number | null>(null);
  maxPrice = signal<number | null>(null);
  furnished = signal(false);
  petsAllowed = signal(false);
  featured = signal(false);

  amenitiesOpen = signal(false);
  moreFiltersOpen = signal(false);
  openDrop = signal<string | null>(null);
  page = signal(1);

  readonly typeOptions = ['Apartment', 'Villa', 'House'];
  readonly bedOptions = [1, 2, 3, 4, 5];

  // ── Price range slider constants ────────────────────────────
  readonly PRICE_MIN  = 0;
  readonly PRICE_MAX  = 500_000;
  readonly PRICE_STEP = 1_000;

  // Percentage positions for the range fill bar
  minPercent = computed(() =>
    ((this.minPrice() ?? this.PRICE_MIN) - this.PRICE_MIN) /
    (this.PRICE_MAX - this.PRICE_MIN) * 100
  );
  maxPercent = computed(() =>
    ((this.maxPrice() ?? this.PRICE_MAX) - this.PRICE_MIN) /
    (this.PRICE_MAX - this.PRICE_MIN) * 100
  );

  typeLabel = computed(() => [...this.types()].join(' · '));
  bedsLabel = computed(() => [...this.beds()].sort((a, b) => a - b).join(', '));
  priceLabel = computed(() => {
    const lo = this.minPrice() !== null ? this.minPrice()!.toLocaleString() : '0';
    const hi = this.maxPrice() !== null ? this.maxPrice()!.toLocaleString() : '∞';
    return `${lo} – ${hi} EGP`;
  });
  moreFiltersCount = computed(() =>
    (this.furnished() ? 1 : 0) +
    (this.petsAllowed() ? 1 : 0) +
    (this.featured() ? 1 : 0) +
    this.cities().size +
    this.amenityFilter().size
  );

  cityOptions = computed(() =>
    [...new Set(this.all().map((p) => p.city).filter(Boolean))].sort()
  );

  private amenityName = computed(() => {
    const map = new Map<string, string>();
    for (const a of this.amenities()) map.set(a.id, a.name);
    return map;
  });

  /** Amenities grouped by category — Lifestyle excluded (covered by Furnished/Pets toggles). */
  amenityGroups = computed(() => {
    const groups = new Map<string, Amenity[]>();
    for (const a of this.amenities()) {
      if (a.category === 'Lifestyle') continue;
      const list = groups.get(a.category) ?? [];
      list.push(a);
      groups.set(a.category, list);
    }
    return [...groups.entries()].map(([category, items]) => ({ category, items }));
  });

  // ── Active filter chips ─────────────────────────────────────────
  chips = computed<Chip[]>(() => {
    const out: Chip[] = [];
    this.types().forEach((t) => out.push({ kind: 'type', value: t, label: t }));
    this.cities().forEach((c) => out.push({ kind: 'city', value: c, label: c }));
    [...this.beds()].sort().forEach((b) => out.push({ kind: 'beds', value: String(b), label: `${b}+ beds` }));
    this.amenityFilter().forEach((id) =>
      out.push({ kind: 'amenity', value: id, label: this.amenityName().get(id) ?? 'Amenity' })
    );
    if (this.minPrice() != null || this.maxPrice() != null) {
      const lo = this.minPrice() != null ? `${this.minPrice()}` : '0';
      const hi = this.maxPrice() != null ? `${this.maxPrice()}` : '∞';
      out.push({ kind: 'price', value: 'price', label: `${lo}–${hi} EGP` });
    }
    if (this.furnished()) out.push({ kind: 'furnished' as any, value: 'furnished', label: 'Furnished' });
    if (this.petsAllowed()) out.push({ kind: 'pets' as any, value: 'pets', label: 'Pets allowed' });
    if (this.featured()) out.push({ kind: 'featured', value: 'featured', label: 'Featured only' });
    return out;
  });

  hasFilters = computed(() => this.chips().length > 0 || !!this.search().trim());

  // ── Filtering ───────────────────────────────────────────────────
  filtered = computed(() => {
    const q = this.search().trim().toLowerCase();
    const types = this.types();
    const cities = this.cities();
    const beds = this.beds();
    const ams = this.amenityFilter();
    const min = this.minPrice();
    const max = this.maxPrice();

    return this.all().filter((p) => {
      if (types.size && !types.has(p.propertyType)) return false;
      if (cities.size && !cities.has(p.city)) return false;
      if (beds.size && !this.matchesBeds(p.bedrooms, beds)) return false;
      if (min != null && p.price < min) return false;
      if (max != null && p.price > max) return false;
      if (ams.size) {
        const ids = new Set(p.amenityIds ?? []);
        for (const a of ams) if (!ids.has(a)) return false;
      }
      if (this.furnished() && !p.isFurnished) return false;
      if (this.petsAllowed() && !p.petsAllowed) return false;
      if (this.featured() && !p.isFeatured) return false;
      if (q) {
        const hay = `${p.title} ${p.titleAr} ${p.city} ${p.district}`.toLowerCase();
        if (!hay.includes(q)) return false;
      }
      return true;
    });
  });

  /** Bedroom set is "N+" matches, so a property passes if it meets any threshold ≤ its count. */
  private matchesBeds(count: number, set: Set<number>): boolean {
    for (const min of set) if (count >= min) return true;
    return false;
  }

  // ── Pagination ──────────────────────────────────────────────────
  totalPages = computed(() => Math.max(1, Math.ceil(this.filtered().length / PAGE_SIZE)));
  paged = computed(() => {
    const start = (this.page() - 1) * PAGE_SIZE;
    return this.filtered().slice(start, start + PAGE_SIZE);
  });
  pageNumbers = computed(() => Array.from({ length: this.totalPages() }, (_, i) => i + 1));

  constructor() {
    // Any filter change resets to page 1
    effect(() => {
      this.search(); this.types(); this.cities(); this.beds();
      this.amenityFilter(); this.minPrice(); this.maxPrice();
      this.furnished(); this.petsAllowed(); this.featured();
      this.page.set(1);
    });
  }

  ngOnInit(): void {
    const qp = this.route.snapshot.queryParamMap;

    // featured deep-link
    if (qp.get('featured') === 'true') this.featured.set(true);
    // furnished deep-link
    if (qp.get('furnished') === 'true') this.furnished.set(true);

    // Location / keyword pre-fill from home-page filter
    const searchParam = qp.get('search');
    if (searchParam) this.search.set(searchParam);

    // Property type pre-selection (comma-separated: ?types=Apartment,Villa)
    const typesParam = qp.get('types');
    if (typesParam) {
      this.types.set(new Set(typesParam.split(',').map(t => t.trim()).filter(Boolean)));
    }

    // Bedrooms pre-selection (?beds=2 → 2+ bed filter)
    const bedsParam = qp.get('beds');
    if (bedsParam) this.beds.set(new Set([Number(bedsParam)]));

    // Price range
    const minP = qp.get('minPrice');
    const maxP = qp.get('maxPrice');
    if (minP) this.minPrice.set(Number(minP));
    if (maxP) this.maxPrice.set(Number(maxP));

    // City pre-selection from top-areas click
    const cityParam = qp.get('city');
    if (cityParam) this.cities.set(new Set([cityParam]));

    this.load();
    this.propertySvc.getAmenities().subscribe({
      next: (list) => this.amenities.set(list),
      error: () => this.amenities.set([]),
    });
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.propertySvc.getActive().subscribe({
      next: (list) => {
        this.all.set(list);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load properties. Please try again.');
        this.loading.set(false);
      },
    });
  }

  // ── Toggle helpers ──────────────────────────────────────────────
  private toggleIn<T>(sig: ReturnType<typeof signal<Set<T>>>, value: T): void {
    const next = new Set(sig());
    next.has(value) ? next.delete(value) : next.add(value);
    sig.set(next);
  }
  toggleType(t: string): void { this.toggleIn(this.types, t); }
  toggleCity(c: string): void { this.toggleIn(this.cities, c); }
  toggleBeds(b: number): void { this.toggleIn(this.beds, b); }
  toggleAmenity(id: string): void { this.toggleIn(this.amenityFilter, id); }

  setSearch(e: Event): void { this.search.set((e.target as HTMLInputElement).value); }
  setMin(e: Event): void { const v = (e.target as HTMLInputElement).value; this.minPrice.set(v ? Number(v) : null); }
  setMax(e: Event): void { const v = (e.target as HTMLInputElement).value; this.maxPrice.set(v ? Number(v) : null); }

  onMinRangeChange(e: Event): void {
    const val = Number((e.target as HTMLInputElement).value);
    const curMax = this.maxPrice() ?? this.PRICE_MAX;
    if (val >= curMax) { (e.target as HTMLInputElement).value = String(curMax - this.PRICE_STEP); return; }
    this.minPrice.set(val <= this.PRICE_MIN ? null : val);
  }

  onMaxRangeChange(e: Event): void {
    const val = Number((e.target as HTMLInputElement).value);
    const curMin = this.minPrice() ?? this.PRICE_MIN;
    if (val <= curMin) { (e.target as HTMLInputElement).value = String(curMin + this.PRICE_STEP); return; }
    this.maxPrice.set(val >= this.PRICE_MAX ? null : val);
  }

  removeChip(chip: Chip): void {
    switch (chip.kind) {
      case 'type': this.toggleType(chip.value); break;
      case 'city': this.toggleCity(chip.value); break;
      case 'beds': this.toggleBeds(Number(chip.value)); break;
      case 'amenity': this.toggleAmenity(chip.value); break;
      case 'price': this.minPrice.set(null); this.maxPrice.set(null); break;
      case 'furnished': this.furnished.set(false); break;
      case 'pets': this.petsAllowed.set(false); break;
      case 'featured': this.featured.set(false); break;
    }
  }

  resetFilters(): void {
    this.search.set('');
    this.types.set(new Set());
    this.cities.set(new Set());
    this.beds.set(new Set());
    this.amenityFilter.set(new Set());
    this.minPrice.set(null);
    this.maxPrice.set(null);
    this.furnished.set(false);
    this.petsAllowed.set(false);
    this.featured.set(false);
  }

  @HostListener('document:click')
  onDocumentClick() { this.openDrop.set(null); }

  toggleDrop(name: string): void {
    this.openDrop.update(cur => cur === name ? null : name);
  }

  goToPage(p: number): void {
    if (p >= 1 && p <= this.totalPages()) {
      this.page.set(p);
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  }

  typeIcon(type: string): string {
    switch (type) {
      case 'Villa': return 'fa-solid fa-house-chimney';
      case 'House': return 'fa-solid fa-house-user';
      default: return 'fa-solid fa-building';
    }
  }

  /** Wishlist toggle — card is a link, so swallow the navigation. */
  toggleSave(p: Property, event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    this.savedSvc.toggle(p);
  }
}
