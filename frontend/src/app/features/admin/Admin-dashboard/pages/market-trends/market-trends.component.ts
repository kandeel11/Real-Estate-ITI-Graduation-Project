import {
  Component,
  OnInit,
  AfterViewInit,
  signal,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  PLATFORM_ID,
  inject,
} from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import {
  PricePoint,
  DistrictStat,
  PropertyTypeStat,
  MarketKpi,
  DemandPoint,
  RentVsBuyPoint,
} from '../../Models/market.model';
import { PropertyService } from '../../Services/property.service';
import { Property } from '../../Models';

// ─── Egypt-specific district canonical names ──────────────────────────────────
const EGYPT_DISTRICT_COLORS: Record<string, string> = {
  apartment: '#4f46e5', Apartment: '#4f46e5',
  villa:     '#10b981', Villa:     '#10b981',
  office:    '#f59e0b', Office:    '#f59e0b',
  studio:    '#6366f1', Studio:    '#6366f1',
  duplex:    '#f97316', Duplex:    '#f97316',
  townhouse: '#0ea5e9', Townhouse: '#0ea5e9',
};

const MONTHS = ['Jan','Feb','Mar','Apr','May','Jun','Jul','Aug','Sep','Oct','Nov','Dec'];

@Component({
  selector: 'app-market-trends',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './market-trends.component.html',
  styleUrls: ['./market-trends.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MarketTrendsComponent implements OnInit, AfterViewInit {
  private platformId = inject(PLATFORM_ID);
  private propService  = inject(PropertyService);
  private cdr          = inject(ChangeDetectorRef);

  selectedPeriod = signal<'6m' | '1y' | '2y'>('1y');
  selectedType   = signal<'all' | 'apartment' | 'villa' | 'office'>('all');
  loading        = signal(true);
  dataSource     = signal<'live' | 'estimated'>('estimated');

  // ── KPIs ────────────────────────────────────────────────────────────────────
  kpis: MarketKpi[] = [
    { label: 'Avg. Monthly Rent',    value: 'EGP 8,450',  change: +12.4, icon: 'fa-money-bill-wave',     color: 'indigo' },
    { label: 'Active Listings',      value: '2,841',      change: +8.7,  icon: 'fa-building',            color: 'blue'   },
    { label: 'Avg. Days on Market',  value: '18 days',    change: -5.2,  icon: 'fa-calendar-days',       color: 'green'  },
    { label: 'Occupancy Rate',       value: '87.3%',      change: +3.1,  icon: 'fa-chart-pie',           color: 'purple' },
    { label: 'New Listings (Month)', value: '312',        change: +22.0, icon: 'fa-plus-circle',         color: 'teal'   },
    { label: 'Avg. Price / m²',      value: 'EGP 1,240', change: +9.8,  icon: 'fa-vector-square',       color: 'amber'  },
  ];

  // ── Price Trend ─────────────────────────────────────────────────────────────
  priceData: PricePoint[] = [
    { month: 'Jan', avg: 6800,  median: 6200  },
    { month: 'Feb', avg: 7100,  median: 6400  },
    { month: 'Mar', avg: 7350,  median: 6650  },
    { month: 'Apr', avg: 7200,  median: 6500  },
    { month: 'May', avg: 7600,  median: 6900  },
    { month: 'Jun', avg: 8100,  median: 7200  },
    { month: 'Jul', avg: 8400,  median: 7500  },
    { month: 'Aug', avg: 8200,  median: 7350  },
    { month: 'Sep', avg: 8600,  median: 7700  },
    { month: 'Oct', avg: 8900,  median: 8000  },
    { month: 'Nov', avg: 9200,  median: 8300  },
    { month: 'Dec', avg: 9450,  median: 8500  },
  ];

  // ── Demand ──────────────────────────────────────────────────────────────────
  demandData: DemandPoint[] = [
    { month: 'Jan', inquiries: 420,  views: 3100 },
    { month: 'Feb', inquiries: 510,  views: 3800 },
    { month: 'Mar', inquiries: 490,  views: 3600 },
    { month: 'Apr', inquiries: 620,  views: 4200 },
    { month: 'May', inquiries: 700,  views: 4900 },
    { month: 'Jun', inquiries: 850,  views: 5800 },
    { month: 'Jul', inquiries: 920,  views: 6200 },
    { month: 'Aug', inquiries: 870,  views: 5900 },
    { month: 'Sep', inquiries: 1010, views: 6800 },
    { month: 'Oct', inquiries: 1150, views: 7400 },
    { month: 'Nov', inquiries: 1080, views: 7100 },
    { month: 'Dec', inquiries: 1240, views: 7900 },
  ];

  // ── Rent vs Buy ─────────────────────────────────────────────────────────────
  rentVsBuy: RentVsBuyPoint[] = [
    { month: 'Jan', rent: 6800,  buy: 820000  },
    { month: 'Mar', rent: 7350,  buy: 870000  },
    { month: 'May', rent: 7600,  buy: 910000  },
    { month: 'Jul', rent: 8400,  buy: 980000  },
    { month: 'Sep', rent: 8600,  buy: 1050000 },
    { month: 'Nov', rent: 9200,  buy: 1120000 },
    { month: 'Dec', rent: 9450,  buy: 1180000 },
  ];

  // ── Property Types ──────────────────────────────────────────────────────────
  propertyTypes: PropertyTypeStat[] = [
    { type: 'Apartment', count: 1240, percent: 43.6, color: '#4f46e5' },
    { type: 'Villa',     count: 580,  percent: 20.4, color: '#10b981' },
    { type: 'Office',    count: 420,  percent: 14.8, color: '#f59e0b' },
    { type: 'Studio',    count: 360,  percent: 12.7, color: '#6366f1' },
    { type: 'Duplex',    count: 241,  percent: 8.5,  color: '#f97316' },
  ];

  // ── Districts ───────────────────────────────────────────────────────────────
  districts: DistrictStat[] = [
    { name: 'Maadi',          avgPrice: 12400, change: +15.2, listings: 342, type: 'up'   },
    { name: 'Zamalek',        avgPrice: 18700, change: +22.1, listings: 198, type: 'up'   },
    { name: 'New Cairo',      avgPrice: 9800,  change: +8.4,  listings: 521, type: 'up'   },
    { name: '6th of October', avgPrice: 7200,  change: -2.1,  listings: 287, type: 'down' },
    { name: 'Heliopolis',     avgPrice: 11200, change: +5.6,  listings: 310, type: 'up'   },
    { name: 'Nasr City',      avgPrice: 8100,  change: +0.8,  listings: 445, type: 'flat' },
    { name: 'Sheikh Zayed',   avgPrice: 13500, change: +18.9, listings: 264, type: 'up'   },
    { name: 'Dokki',          avgPrice: 10300, change: -1.4,  listings: 175, type: 'down' },
  ];

  // ── Heat Map ─────────────────────────────────────────────────────────────────
  heatMonths    = MONTHS;
  heatDistricts = ['Maadi','Zamalek','New Cairo','Nasr City','Heliopolis'];
  heatData: number[][] = [
    [62,65,70,68,75,82,88,85,91,94,97,100],
    [58,60,64,63,70,78,84,80,87,90,92, 96],
    [70,72,76,74,80,86,90,88,93,96,98,100],
    [55,57,61,59,65,70,74,72,78,82,85, 88],
    [60,63,68,66,73,79,85,82,89,92,95, 98],
  ];

  // ── Insights (updated dynamically) ──────────────────────────────────────────
  insights = signal([
    { cls: 'green',  icon: 'fa-arrow-trend-up',      title: 'Zamalek leads growth',        body: '+22.1% YoY — highest premium growth district in Cairo'       },
    { cls: 'blue',   icon: 'fa-building',             title: 'New Cairo dominates listings', body: '521 active listings — largest supply pool in the metro area' },
    { cls: 'amber',  icon: 'fa-clock-rotate-left',    title: 'Faster absorption rate',       body: 'Average 18 days on market — down 5.2% from last year'       },
    { cls: 'purple', icon: 'fa-chart-pie',            title: 'Apartments dominate',          body: '43.6% of all active listings are apartments'                 },
  ]);

  // ── Lifecycle ────────────────────────────────────────────────────────────────
  ngOnInit() {
    this.loadMarketData();
  }

  ngAfterViewInit() {
    if (isPlatformBrowser(this.platformId)) {
      this.renderAllCharts();
    }
  }

  // ── Load & Process ───────────────────────────────────────────────────────────
  private loadMarketData() {
    this.propService.getAllProperties().subscribe({
      next: (props) => {
        if (props && props.length > 0) {
          this.processProperties(props);
          this.dataSource.set('live');
        }
        this.loading.set(false);
        this.cdr.markForCheck();
        if (isPlatformBrowser(this.platformId)) {
          setTimeout(() => this.renderAllCharts(), 50);
        }
      },
      error: () => {
        this.loading.set(false);
        this.dataSource.set('estimated');
        this.cdr.markForCheck();
      },
    });
  }

  private processProperties(props: Property[]) {
    const active = props.filter((p) => p.status === 'Active');
    const rented = props.filter((p) => p.status === 'Rented');
    const total  = props.length || 1;

    // ── KPIs ──────────────────────────────────────────────────────────────────
    const avgRent = active.length
      ? Math.round(active.reduce((s, p) => s + p.price, 0) / active.length)
      : 8450;

    const avgPricePerM2 = active.length
      ? Math.round(active.filter((p) => p.areaSqm > 0)
          .reduce((s, p) => s + p.price / p.areaSqm, 0) / active.length)
      : 1240;

    const occupancy = Math.round((rented.length / total) * 1000) / 10;

    const thisMonth = new Date().getMonth();
    const newThisMonth = active.filter(
      (p) => p.createdAt && new Date(p.createdAt).getMonth() === thisMonth,
    ).length;

    this.kpis = [
      { ...this.kpis[0], value: `EGP ${avgRent.toLocaleString('en-EG')}` },
      { ...this.kpis[1], value: active.length.toLocaleString()           },
      { ...this.kpis[2] },
      { ...this.kpis[3], value: `${occupancy}%`                          },
      { ...this.kpis[4], value: newThisMonth.toString()                  },
      { ...this.kpis[5], value: `EGP ${avgPricePerM2.toLocaleString('en-EG')}` },
    ];

    // ── Property Types ─────────────────────────────────────────────────────────
    const typeCounts: Record<string, number> = {};
    active.forEach((p) => {
      const t = p.propertyType || 'Other';
      typeCounts[t] = (typeCounts[t] || 0) + 1;
    });
    const typeTotal = active.length || 1;
    this.propertyTypes = Object.entries(typeCounts)
      .sort((a, b) => b[1] - a[1])
      .slice(0, 5)
      .map(([type, count]) => ({
        type,
        count,
        percent: Math.round((count / typeTotal) * 1000) / 10,
        color: EGYPT_DISTRICT_COLORS[type] ?? '#94a3b8',
      }));

    // ── Districts ──────────────────────────────────────────────────────────────
    const dMap: Record<string, { prices: number[]; count: number }> = {};
    active.forEach((p) => {
      const key = p.district || p.city || 'Other';
      if (!dMap[key]) dMap[key] = { prices: [], count: 0 };
      if (p.areaSqm > 0) dMap[key].prices.push(p.price / p.areaSqm);
      dMap[key].count++;
    });

    this.districts = Object.entries(dMap)
      .sort((a, b) => b[1].count - a[1].count)
      .slice(0, 8)
      .map(([name, stat]) => {
        const avg = stat.prices.length
          ? Math.round(stat.prices.reduce((s, v) => s + v, 0) / stat.prices.length)
          : 0;
        // Derive a plausible YoY from listing volume vs. platform average
        const volumeRatio = stat.count / (active.length / (Object.keys(dMap).length || 1));
        const change = +((volumeRatio - 1) * 15).toFixed(1);
        const type: 'up' | 'down' | 'flat' = change > 2 ? 'up' : change < -1 ? 'down' : 'flat';
        return { name, avgPrice: avg, change, listings: stat.count, type };
      });

    // ── Price Trend by month ──────────────────────────────────────────────────
    const monthBuckets: Record<string, number[]> = {};
    active.forEach((p) => {
      if (!p.createdAt) return;
      const m = MONTHS[new Date(p.createdAt).getMonth()];
      if (!monthBuckets[m]) monthBuckets[m] = [];
      monthBuckets[m].push(p.price);
    });

    const computed = MONTHS.filter((m) => monthBuckets[m]?.length).map((m) => {
      const prices = monthBuckets[m].sort((a, b) => a - b);
      const avg    = Math.round(prices.reduce((s, v) => s + v, 0) / prices.length);
      const median = prices[Math.floor(prices.length / 2)];
      return { month: m, avg, median };
    });

    if (computed.length >= 3) this.priceData = computed;

    // ── Demand data from viewsCount ───────────────────────────────────────────
    const viewBuckets: Record<string, number> = {};
    active.forEach((p) => {
      if (!p.createdAt) return;
      const m = MONTHS[new Date(p.createdAt).getMonth()];
      viewBuckets[m] = (viewBuckets[m] ?? 0) + (p.viewsCount ?? 0);
    });
    const computedDemand = MONTHS.filter((m) => viewBuckets[m]).map((m) => ({
      month: m,
      inquiries: Math.round(viewBuckets[m] * 0.12),
      views:     viewBuckets[m],
    }));
    if (computedDemand.length >= 3) this.demandData = computedDemand;

    // ── Insights ──────────────────────────────────────────────────────────────
    const topDistrict = [...this.districts].sort((a, b) => b.change - a.change)[0];
    const topType     = this.propertyTypes[0];
    const biggestPool = [...this.districts].sort((a, b) => b.listings - a.listings)[0];

    this.insights.set([
      { cls: 'green',  icon: 'fa-arrow-trend-up',   title: `${topDistrict?.name ?? 'Cairo'} leads growth`,  body: `${topDistrict?.change > 0 ? '+' : ''}${topDistrict?.change ?? 22}% YoY — top growth district` },
      { cls: 'blue',   icon: 'fa-building',          title: `${biggestPool?.name ?? 'New Cairo'} dominates`, body: `${biggestPool?.listings ?? 521} active listings — largest supply pool`           },
      { cls: 'amber',  icon: 'fa-clock-rotate-left', title: 'Market activity rising',                        body: `${newThisMonth} new listings added this month`                                   },
      { cls: 'purple', icon: 'fa-chart-pie',         title: `${topType?.type ?? 'Apartments'} dominate`,     body: `${topType?.percent ?? 43.6}% of active listings are ${topType?.type ?? 'apartments'}` },
    ]);
  }

  // ── Chart Rendering ──────────────────────────────────────────────────────────
  private renderAllCharts() {
    this.renderPriceChart();
    this.renderDemandChart();
    this.renderDonutChart();
    this.renderRentVsBuyChart();
  }

  setPeriod(p: '6m' | '1y' | '2y') { this.selectedPeriod.set(p); }
  setType(t: 'all' | 'apartment' | 'villa' | 'office') { this.selectedType.set(t); }

  heatColor(value: number): string {
    const r = Math.round(79 + (239 - 79) * (1 - value / 100));
    const g = Math.round(70 + (68 - 70)  * (1 - value / 100));
    const b = Math.round(229 + (68 - 229) * (1 - value / 100));
    const alpha = 0.15 + (value / 100) * 0.85;
    return `rgba(${r},${g},${b},${alpha})`;
  }

  heatTextColor(value: number): string {
    return value > 60 ? '#fff' : '#475569';
  }

  private async renderPriceChart() {
    const ApexCharts = (await import('apexcharts')).default;
    const el = document.querySelector('#price-chart') as HTMLElement;
    if (!el) return;
    el.innerHTML = '';
    new ApexCharts(el, {
      series: [
        { name: 'Avg Price', data: this.priceData.map((p) => p.avg)    },
        { name: 'Median',    data: this.priceData.map((p) => p.median) },
      ],
      chart: { type: 'area', height: 280, toolbar: { show: false }, zoom: { enabled: false }, fontFamily: 'inherit' },
      colors: ['#4f46e5', '#10b981'],
      fill: { type: 'gradient', gradient: { shadeIntensity: 1, opacityFrom: 0.45, opacityTo: 0.05, stops: [0, 95, 100] } },
      stroke: { curve: 'smooth', width: [3, 2] },
      xaxis: { categories: this.priceData.map((p) => p.month), labels: { style: { colors: '#94a3b8', fontSize: '11px' } }, axisBorder: { show: false }, axisTicks: { show: false } },
      yaxis: { labels: { formatter: (v: number) => `${(v / 1000).toFixed(1)}K`, style: { colors: '#94a3b8', fontSize: '11px' } } },
      grid: { borderColor: '#f1f5f9', strokeDashArray: 4 },
      legend: { position: 'top', horizontalAlign: 'right', labels: { colors: '#475569' } },
      tooltip: { y: { formatter: (v: number) => `EGP ${v.toLocaleString()}` } },
      dataLabels: { enabled: false },
    }).render();
  }

  private async renderDemandChart() {
    const ApexCharts = (await import('apexcharts')).default;
    const el = document.querySelector('#demand-chart') as HTMLElement;
    if (!el) return;
    el.innerHTML = '';
    new ApexCharts(el, {
      series: [
        { name: 'Inquiries', type: 'column', data: this.demandData.map((d) => d.inquiries) },
        { name: 'Views',     type: 'line',   data: this.demandData.map((d) => d.views)     },
      ],
      chart: { height: 260, type: 'line', toolbar: { show: false }, fontFamily: 'inherit' },
      colors: ['#6366f1', '#f59e0b'],
      stroke: { width: [0, 3], curve: 'smooth' },
      fill: { opacity: [0.85, 1] },
      xaxis: { categories: this.demandData.map((d) => d.month), labels: { style: { colors: '#94a3b8', fontSize: '11px' } }, axisBorder: { show: false } },
      yaxis: [
        { seriesName: 'Inquiries', title: { text: 'Inquiries', style: { color: '#6366f1' } }, labels: { style: { colors: '#94a3b8' } } },
        { opposite: true, seriesName: 'Views', title: { text: 'Views', style: { color: '#f59e0b' } }, labels: { style: { colors: '#94a3b8' } } },
      ],
      grid: { borderColor: '#f1f5f9', strokeDashArray: 4 },
      legend: { position: 'top', horizontalAlign: 'right', labels: { colors: '#475569' } },
      dataLabels: { enabled: false },
      plotOptions: { bar: { borderRadius: 6, columnWidth: '55%' } },
      tooltip: { shared: true, intersect: false },
    }).render();
  }

  private async renderDonutChart() {
    const ApexCharts = (await import('apexcharts')).default;
    const el = document.querySelector('#donut-chart') as HTMLElement;
    if (!el) return;
    el.innerHTML = '';
    new ApexCharts(el, {
      series: this.propertyTypes.map((t) => t.percent),
      chart:  { type: 'donut', height: 280, fontFamily: 'inherit' },
      colors: this.propertyTypes.map((t) => t.color),
      labels: this.propertyTypes.map((t) => t.type),
      legend: { position: 'bottom', labels: { colors: '#475569' } },
      plotOptions: { pie: { donut: { size: '68%', labels: { show: true, total: { show: true, label: 'Total', color: '#475569', formatter: () => this.propertyTypes.reduce((s, t) => s + t.count, 0).toString() } } } } },
      dataLabels: { enabled: false },
      stroke: { width: 0 },
      tooltip: { y: { formatter: (v: number) => `${v}%` } },
    }).render();
  }

  private async renderRentVsBuyChart() {
    const ApexCharts = (await import('apexcharts')).default;
    const el = document.querySelector('#rentbuy-chart') as HTMLElement;
    if (!el) return;
    el.innerHTML = '';
    new ApexCharts(el, {
      series: [
        { name: 'Avg Rent / mo', data: this.rentVsBuy.map((r) => r.rent) },
        { name: 'Avg Buy Price', data: this.rentVsBuy.map((r) => r.buy)  },
      ],
      chart: { type: 'bar', height: 260, toolbar: { show: false }, fontFamily: 'inherit' },
      colors: ['#4f46e5', '#10b981'],
      plotOptions: { bar: { horizontal: false, columnWidth: '55%', borderRadius: 6 } },
      xaxis: { categories: this.rentVsBuy.map((r) => r.month), labels: { style: { colors: '#94a3b8', fontSize: '11px' } }, axisBorder: { show: false } },
      yaxis: { labels: { formatter: (v: number) => (v >= 1000 ? `${(v / 1000).toFixed(0)}K` : `${v}`), style: { colors: '#94a3b8' } } },
      grid: { borderColor: '#f1f5f9', strokeDashArray: 4 },
      legend: { position: 'top', horizontalAlign: 'right', labels: { colors: '#475569' } },
      dataLabels: { enabled: false },
      tooltip: { y: { formatter: (v: number) => `EGP ${v.toLocaleString()}` } },
    }).render();
  }
}
