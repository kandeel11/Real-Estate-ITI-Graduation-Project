import { Component, inject, signal } from '@angular/core';
import {
  NgApexchartsModule,
  ApexAxisChartSeries,
  ApexChart,
  ApexXAxis,
  ApexFill,
  ApexStroke,
  ApexDataLabels,
  ApexTooltip,
  ApexGrid,
  ApexNonAxisChartSeries,
  ApexLegend,
  ApexPlotOptions,
} from 'ng-apexcharts';
import { AdminDashboardService } from '../../Services/admin-dashboard';
import { DashboardStats, PendingProperty, RentPayment, AdminUser } from '../../Models';
import { DecimalPipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [NgApexchartsModule, DecimalPipe, TranslatePipe],
  templateUrl: './dashboard.html',
  styleUrls: ['./dashboard.scss'],
})
export class DashboardComponent {
  private service = inject(AdminDashboardService);

  stats = signal<DashboardStats | null>(null);
  pendingProperties = signal<PendingProperty[]>([]);
  recentPayments = signal<RentPayment[]>([]);
  latestUsers = signal<AdminUser[]>([]);

  financialChart: any = null;
  occupancyChart: any = null;

  occupiedCount = signal(39);
  vacantCount = signal(3);
  occupancyPercent = signal(92);

  constructor() {
    this.loadAll();
  }

  loadAll() {
    this.service.getStats().subscribe((s) => this.stats.set(s));

    this.service.getMonthlyRevenue().subscribe((data) => {
      const months = data.slice(0, 8).map((d) => d.month);
      const revenue = data.slice(0, 8).map((d) => d.amount);
      const expenses = revenue.map((v) => Math.round(v * 0.35 + Math.random() * 2000));

      this.financialChart = {
        series: [
          { name: 'Revenue', type: 'column', data: revenue },
          { name: 'Expenses', type: 'line', data: expenses },
        ] as ApexAxisChartSeries,
        chart: {
          height: 340,
          type: 'line',
          toolbar: { show: false },
          fontFamily: 'inherit',
          stacked: false,
        } as ApexChart,
        colors: ['#a5a4f9', '#ef4444'],
        stroke: {
          width: [0, 3],
          curve: 'smooth',
        } as ApexStroke,
        plotOptions: {
          bar: { columnWidth: '45%', borderRadius: 6 },
        } as ApexPlotOptions,
        fill: {
          type: ['gradient', 'solid'],
          gradient: {
            shade: 'light',
            type: 'vertical',
            shadeIntensity: 0.25,
            gradientToColors: ['#6366f1'],
            opacityFrom: 1,
            opacityTo: 0.8,
            stops: [0, 100],
          },
        } as ApexFill,
        dataLabels: { enabled: false } as ApexDataLabels,
        markers: {
          size: 5,
          colors: ['#fff'],
          strokeColors: '#ef4444',
          strokeWidth: 2.5,
          hover: { size: 7 },
        },
        xaxis: {
          categories: months,
          labels: { style: { colors: '#94a3b8', fontSize: '12px', fontWeight: 500 } },
          axisBorder: { show: false },
          axisTicks: { show: false },
        } as ApexXAxis,
        yaxis: {
          labels: {
            style: { colors: '#94a3b8', fontSize: '12px' },
            formatter: (val: number) => '$' + (val / 1000).toFixed(0) + 'K',
          },
        },
        grid: {
          borderColor: '#eef0f5',
          strokeDashArray: 5,
          xaxis: { lines: { show: false } },
          padding: { left: 10, right: 10 },
        } as ApexGrid,
        legend: { show: false } as ApexLegend,
        tooltip: {
          shared: true,
          intersect: false,
          y: { formatter: (val: number) => '$' + val.toLocaleString() },
        } as ApexTooltip,
      };
    });

    // Occupancy donut
    this.occupancyChart = {
      series: [this.occupancyPercent(), 100 - this.occupancyPercent()] as ApexNonAxisChartSeries,
      chart: {
        type: 'donut',
        height: 280,
        fontFamily: 'inherit',
        sparkline: { enabled: false },
      } as ApexChart,
      labels: ['Occupied', 'Vacant'],
      colors: ['#4f46e5', '#e0e7ff'],
      stroke: { width: 0 } as ApexStroke,
      legend: { show: false } as ApexLegend,
      dataLabels: { enabled: false } as ApexDataLabels,
      plotOptions: {
        pie: {
          donut: {
            size: '78%',
            labels: {
              show: true,
              name: { show: false },
              value: {
                show: true,
                fontSize: '36px',
                fontWeight: 800,
                color: '#0f172a',
                offsetY: -8,
                formatter: (val) => `${val}%`,
              },
              total: {
                show: true,
                label: 'Leased',
                fontSize: '13px',
                fontWeight: 500,
                color: '#94a3b8',
                formatter: () => `${this.occupancyPercent()}%`,
              },
            },
          },
        },
      } as ApexPlotOptions,
    };

    this.service
      .getPendingProperties()
      .subscribe((data) => this.pendingProperties.set(data.slice(0, 2)));

    this.service.getRecentPayments().subscribe((data) => this.recentPayments.set(data.slice(0, 3)));
  }

  approve(p: PendingProperty) {
    this.service
      .approveProperty(p.id)
      .subscribe(() => this.pendingProperties.update((list) => list.filter((x) => x.id !== p.id)));
  }

  reject(p: PendingProperty) {
    this.service
      .rejectProperty(p.id)
      .subscribe(() => this.pendingProperties.update((list) => list.filter((x) => x.id !== p.id)));
  }

  formatMoney(price: number): string {
    return price.toLocaleString('en-US');
  }

  timeAgo(date: string): string {
    const diff = (Date.now() - new Date(date).getTime()) / 1000;
    if (diff < 60) return 'Just now';
    if (diff < 3600) return `${Math.floor(diff / 60)} mins ago`;
    if (diff < 86400) return `${Math.floor(diff / 3600)} hours ago`;
    const days = Math.floor(diff / 86400);
    return `${days} day${days > 1 ? 's' : ''} ago`;
  }
}
