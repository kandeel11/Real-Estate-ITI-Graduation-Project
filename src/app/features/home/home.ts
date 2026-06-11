import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { InViewDirective } from '../../shared/directives/in-view.directive';
import { SearchService } from '../../features/ai/Search/Services/search-service';
import { ParsedQuery, PropertyResult } from '../../features/ai/Search/models';

type SearchMode = 'regular' | 'smart';

@Component({
  selector: 'app-home',
  imports: [InViewDirective],
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class HomeComponent {
  // ============================================================
  // 🔎 SEARCH STATE (NEW)
  // ============================================================
  private searchService = inject(SearchService);
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
    {
      icon: 'fa-solid fa-house',
      title: 'Find Your Dream Home',
      desc: 'Explore thousands of listings tailored to your lifestyle and budget.',
    },
    {
      icon: 'fa-solid fa-coins',
      title: 'Unlock Property Value',
      desc: 'Get accurate valuations and sell your property for top dollar.',
    },
    {
      icon: 'fa-solid fa-building',
      title: 'Property Management',
      desc: 'Hassle-free management services for landlords and investors.',
    },
    {
      icon: 'fa-solid fa-chart-line',
      title: 'Smart Investments',
      desc: 'Data-driven insights to make informed real estate decisions.',
    },
  ];

  properties = [
    {
      image:
        'https://lh3.googleusercontent.com/aida/AP1WRLtXTqG-OryxK0IDEFhDE-rC7mkUbJfWGs-PxU1pRNe2uSE0Bx_3c-cP1F4SR7ZMhcSiRCbULkJJ1tOmGSwv7Fzt8TlqRd3pDTgij-4202Hslb__YtSs9qdb0Fs53Kase1pwHmioWuh3lovZkjDx78oMKMg1zkEJrO1qFm3XUJ9q33UDNvEhsf5tmaSfZajm7zZnYRa68Zmm1FYqdAj-HCcuoBsbv2fmoY88tkqk6Ui-ucxSVfXPbrLTF8A',
      badge: 'For Sale',
      name: 'Seaside Serenity Villa',
      desc: 'A stunning 4-bedroom, 3-bathroom villa in a peaceful suburban neighborhood offering tranquil sea views.',
      beds: 4,
      baths: '3',
      typeIcon: 'fa-solid fa-house-chimney',
      type: 'Villa',
      price: '$550,000',
    },
    {
      image:
        'https://lh3.googleusercontent.com/aida/AP1WRLtd6UB33H6m-txrdPaoya8P7oMmzNLMtgyhQQ6quatx9iK8pfDZw48sojNSK_HoGokwl8unHZW0yY-8J7BJMpshFDDLL2myoGdA3gynnj364NfrgzPia5ynkXmOspBPnTPzULhB-8eC9E4LqHUq1T5MukTkNfM7KYNl9W44BCljwqV-PHVW4_I31tGUmgEQHh8AeUgEiUNUherzbtvYMcpsxE4mqaRP6GZ8aXNPPYM2ccP2yFwZOdN1-w',
      badge: 'For Sale',
      name: 'Metropolitan Haven',
      desc: 'A chic and fully-furnished 2-bedroom apartment with panoramic city views in the heart of downtown.',
      beds: 2,
      baths: '2',
      typeIcon: 'fa-solid fa-building',
      type: 'Apartment',
      price: '$650,000',
    },
    {
      image:
        'https://lh3.googleusercontent.com/aida/AP1WRLt2tMGhhSeq2ZqItXJH80ZtMP-QyYlj7uCCDmT_qDLmf0YQYDAzLBYRDeWw8J4b8Pkkk9Awqx2SGKUB8oDHoq7-kg0s3f6y1AghALZEa5Xu1eIB8EIqWKBsps6OQ7GZFzQ2R2A6noXc2d3Qc8VCRTYklAAACnrahKxVpw-FsYf3bCof9rnKESiMxD0BTjnqxDsrbxDBCp5Yptr8C6RMafRPX5cNSwISSfTlUUzqK9Aq37MohNWHxOyMzMs',
      badge: 'For Sale',
      name: 'Suburban Retreat',
      desc: 'An elegant 3-bedroom, 2.5-bathroom townhouse in a gated community offering privacy and top-tier amenities.',
      beds: 3,
      baths: '2.5',
      typeIcon: 'fa-solid fa-house-user',
      type: 'Townhouse',
      price: '$450,000',
    },
  ];

  faqs = [
    {
      q: 'How do I start searching for a property?',
      a: 'Begin by using our search bar to filter by location, property type, or price range. You can also browse featured listings or reach out to one of our expert brokers for personalized guidance.',
    },
    {
      q: 'What is the process for buying a property through Estatein?',
      a: 'Our process is simple: search and shortlist properties, schedule a viewing, submit an offer, and our team will guide you through due diligence, financing, and closing — every step of the way.',
    },
    {
      q: 'Are there financing or mortgage options available?',
      a: 'Yes. We partner with leading financial institutions to offer competitive mortgage rates. Our advisors can help you explore financing options tailored to your budget and credit profile at no extra cost.',
    },
    {
      q: 'How does property valuation work?',
      a: 'Our certified appraisers conduct a thorough market analysis comparing recent sales, property condition, location desirability, and amenities. You receive a detailed report within 48 hours.',
    },
    {
      q: 'Can I list my property for sale or rent on Estatein?',
      a: 'Absolutely. Register as a property owner or broker, create your listing with photos and details, and our team will review and publish it within 24 hours. Premium featured placement is also available.',
    },
    {
      q: 'What fees does Estatein charge buyers and sellers?',
      a: 'Browsing and searching is always free. For sellers and landlords, we charge a competitive commission only upon a successful transaction. Contact us for a full fee schedule breakdown.',
    },
  ];

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
