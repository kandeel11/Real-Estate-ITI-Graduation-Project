import {
  Component,
  OnInit,
  AfterViewInit,
  signal,
  ChangeDetectionStrategy,
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

  selectedPeriod = signal<'6m' | '1y' | '2y'>('1y');
  selectedType = signal<'all' | 'apartment' | 'villa' | 'office'>('all');

  // ===== KPIs =====
  kpis: MarketKpi[] = [
    {
      label: 'Avg. Monthly Rent',
      value: 'EGP 8,450',
      change: +12.4,
      icon: 'fa-money-bill-wave',
      color: 'indigo',
    },
    { label: 'Active Listings', value: '2,841', change: +8.7, icon: 'fa-building', color: 'blue' },
    {
      label: 'Avg. Days on Market',
      value: '18 days',
      change: -5.2,
      icon: 'fa-calendar-days',
      color: 'green',
    },
    {
      label: 'Occupancy Rate',
      value: '87.3%',
      change: +3.1,
      icon: 'fa-chart-pie',
      color: 'purple',
    },
    {
      label: 'New Listings (Month)',
      value: '312',
      change: +22.0,
      icon: 'fa-plus-circle',
      color: 'teal',
    },
    {
      label: 'Avg. Price / m²',
      value: 'EGP 1,240',
      change: +9.8,
      icon: 'fa-vector-square',
      color: 'amber',
    },
  ];

  // ===== Price Trend =====
  priceData: PricePoint[] = [
    { month: 'Jan', avg: 6800, median: 6200 },
    { month: 'Feb', avg: 7100, median: 6400 },
    { month: 'Mar', avg: 7350, median: 6650 },
    { month: 'Apr', avg: 7200, median: 6500 },
    { month: 'May', avg: 7600, median: 6900 },
    { month: 'Jun', avg: 8100, median: 7200 },
    { month: 'Jul', avg: 8400, median: 7500 },
    { month: 'Aug', avg: 8200, median: 7350 },
    { month: 'Sep', avg: 8600, median: 7700 },
    { month: 'Oct', avg: 8900, median: 8000 },
    { month: 'Nov', avg: 9200, median: 8300 },
    { month: 'Dec', avg: 9450, median: 8500 },
  ];

  // ===== Demand =====
  demandData: DemandPoint[] = [
    { month: 'Jan', inquiries: 420, views: 3100 },
    { month: 'Feb', inquiries: 510, views: 3800 },
    { month: 'Mar', inquiries: 490, views: 3600 },
    { month: 'Apr', inquiries: 620, views: 4200 },
    { month: 'May', inquiries: 700, views: 4900 },
    { month: 'Jun', inquiries: 850, views: 5800 },
    { month: 'Jul', inquiries: 920, views: 6200 },
    { month: 'Aug', inquiries: 870, views: 5900 },
    { month: 'Sep', inquiries: 1010, views: 6800 },
    { month: 'Oct', inquiries: 1150, views: 7400 },
    { month: 'Nov', inquiries: 1080, views: 7100 },
    { month: 'Dec', inquiries: 1240, views: 7900 },
  ];

  // ===== Rent vs Buy =====
  rentVsBuy: RentVsBuyPoint[] = [
    { month: 'Jan', rent: 6800, buy: 820000 },
    { month: 'Mar', rent: 7350, buy: 870000 },
    { month: 'May', rent: 7600, buy: 910000 },
    { month: 'Jul', rent: 8400, buy: 980000 },
    { month: 'Sep', rent: 8600, buy: 1050000 },
    { month: 'Nov', rent: 9200, buy: 1120000 },
    { month: 'Dec', rent: 9450, buy: 1180000 },
  ];

  // ===== Property Types =====
  propertyTypes: PropertyTypeStat[] = [
    { type: 'Apartment', count: 1240, percent: 43.6, color: '#4f46e5' },
    { type: 'Villa', count: 580, percent: 20.4, color: '#10b981' },
    { type: 'Office', count: 420, percent: 14.8, color: '#f59e0b' },
    { type: 'Studio', count: 360, percent: 12.7, color: '#6366f1' },
    { type: 'Duplex', count: 241, percent: 8.5, color: '#f97316' },
  ];

  // ===== Districts =====
  districts: DistrictStat[] = [
    { name: 'Maadi', avgPrice: 12400, change: +15.2, listings: 342, type: 'up' },
    { name: 'Zamalek', avgPrice: 18700, change: +22.1, listings: 198, type: 'up' },
    { name: 'New Cairo', avgPrice: 9800, change: +8.4, listings: 521, type: 'up' },
    { name: '6th of October', avgPrice: 7200, change: -2.1, listings: 287, type: 'down' },
    { name: 'Heliopolis', avgPrice: 11200, change: +5.6, listings: 310, type: 'up' },
    { name: 'Nasr City', avgPrice: 8100, change: +0.8, listings: 445, type: 'flat' },
    { name: 'Sheikh Zayed', avgPrice: 13500, change: +18.9, listings: 264, type: 'up' },
    { name: 'Dokki', avgPrice: 10300, change: -1.4, listings: 175, type: 'down' },
  ];

  // ===== Heat Map: Month x District =====
  heatMonths = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
  heatDistricts = ['Maadi', 'Zamalek', 'New Cairo', 'Nasr City', 'Heliopolis'];
  heatData: number[][] = [
    [62, 65, 70, 68, 75, 82, 88, 85, 91, 94, 97, 100],
    [58, 60, 64, 63, 70, 78, 84, 80, 87, 90, 92, 96],
    [70, 72, 76, 74, 80, 86, 90, 88, 93, 96, 98, 100],
    [55, 57, 61, 59, 65, 70, 74, 72, 78, 82, 85, 88],
    [60, 63, 68, 66, 73, 79, 85, 82, 89, 92, 95, 98],
  ];

  ngOnInit() {}

  ngAfterViewInit() {
    if (isPlatformBrowser(this.platformId)) {
      this.renderPriceChart();
      this.renderDemandChart();
      this.renderDonutChart();
      this.renderRentVsBuyChart();
    }
  }

  setPeriod(p: '6m' | '1y' | '2y') {
    this.selectedPeriod.set(p);
  }
  setType(t: 'all' | 'apartment' | 'villa' | 'office') {
    this.selectedType.set(t);
  }

  heatColor(value: number): string {
    const r = Math.round(79 + (239 - 79) * (1 - value / 100));
    const g = Math.round(70 + (68 - 70) * (1 - value / 100));
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
        { name: 'Avg Price', data: this.priceData.map((p) => p.avg) },
        { name: 'Median', data: this.priceData.map((p) => p.median) },
      ],
      chart: {
        type: 'area',
        height: 280,
        toolbar: { show: false },
        zoom: { enabled: false },
        fontFamily: 'inherit',
      },
      colors: ['#4f46e5', '#10b981'],
      fill: {
        type: 'gradient',
        gradient: { shadeIntensity: 1, opacityFrom: 0.45, opacityTo: 0.05, stops: [0, 95, 100] },
      },
      stroke: { curve: 'smooth', width: [3, 2] },
      xaxis: {
        categories: this.priceData.map((p) => p.month),
        labels: { style: { colors: '#94a3b8', fontSize: '11px' } },
        axisBorder: { show: false },
        axisTicks: { show: false },
      },
      yaxis: {
        labels: {
          formatter: (v: number) => `${(v / 1000).toFixed(1)}K`,
          style: { colors: '#94a3b8', fontSize: '11px' },
        },
      },
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
        { name: 'Views', type: 'line', data: this.demandData.map((d) => d.views) },
      ],
      chart: { height: 260, type: 'line', toolbar: { show: false }, fontFamily: 'inherit' },
      colors: ['#6366f1', '#f59e0b'],
      stroke: { width: [0, 3], curve: 'smooth' },
      fill: { opacity: [0.85, 1] },
      xaxis: {
        categories: this.demandData.map((d) => d.month),
        labels: { style: { colors: '#94a3b8', fontSize: '11px' } },
        axisBorder: { show: false },
      },
      yaxis: [
        {
          seriesName: 'Inquiries',
          title: { text: 'Inquiries', style: { color: '#6366f1' } },
          labels: { style: { colors: '#94a3b8' } },
        },
        {
          opposite: true,
          seriesName: 'Views',
          title: { text: 'Views', style: { color: '#f59e0b' } },
          labels: { style: { colors: '#94a3b8' } },
        },
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
      chart: { type: 'donut', height: 280, fontFamily: 'inherit' },
      colors: this.propertyTypes.map((t) => t.color),
      labels: this.propertyTypes.map((t) => t.type),
      legend: { position: 'bottom', labels: { colors: '#475569' } },
      plotOptions: {
        pie: {
          donut: {
            size: '68%',
            labels: {
              show: true,
              total: { show: true, label: 'Total', color: '#475569', formatter: () => '2,841' },
            },
          },
        },
      },
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
        { name: 'Avg Buy Price', data: this.rentVsBuy.map((r) => r.buy) },
      ],
      chart: { type: 'bar', height: 260, toolbar: { show: false }, fontFamily: 'inherit' },
      colors: ['#4f46e5', '#10b981'],
      plotOptions: {
        bar: { horizontal: false, columnWidth: '55%', borderRadius: 6 },
      },
      xaxis: {
        categories: this.rentVsBuy.map((r) => r.month),
        labels: { style: { colors: '#94a3b8', fontSize: '11px' } },
        axisBorder: { show: false },
      },
      yaxis: {
        labels: {
          formatter: (v: number) => (v >= 1000 ? `${(v / 1000).toFixed(0)}K` : `${v}`),
          style: { colors: '#94a3b8' },
        },
      },
      grid: { borderColor: '#f1f5f9', strokeDashArray: 4 },
      legend: { position: 'top', horizontalAlign: 'right', labels: { colors: '#475569' } },
      dataLabels: { enabled: false },
      tooltip: { y: { formatter: (v: number) => `EGP ${v.toLocaleString()}` } },
    }).render();
  }
}
