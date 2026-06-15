import {
  Component,
  inject,
  signal,
  AfterViewInit,
  OnDestroy,
  ViewChild,
  ElementRef,
} from '@angular/core';
import ApexCharts from 'apexcharts';
import { MaintenancePortalService } from '../services/maintenance-portal.service';
import { CategoryEarnings, Ticket, TicketStats, TicketStatus, PaymentStatus } from '../models';
import { DecimalPipe } from '@angular/common';

@Component({
  selector: 'app-maintenance-overview',
  standalone: true,
  imports: [DecimalPipe],
  templateUrl: './overview.component.html',
  styleUrls: ['./overview.component.scss'],
})
export class OverviewComponent implements AfterViewInit, OnDestroy {
  private service = inject(MaintenancePortalService);

  @ViewChild('earningsChartEl') earningsChartEl!: ElementRef<HTMLElement>;
  @ViewChild('statusChartEl') statusChartEl!: ElementRef<HTMLElement>;

  stats = signal<TicketStats | null>(null);
  categories = signal<CategoryEarnings[]>([]);
  recentTickets = signal<Ticket[]>([]);

  private earningsChart: ApexCharts | null = null;
  private statusChart: ApexCharts | null = null;

  ngAfterViewInit() {
    this.loadAll();
  }

  ngOnDestroy() {
    this.earningsChart?.destroy();
    this.statusChart?.destroy();
  }

  loadAll() {
    this.service.getStats().subscribe((s) => {
      this.stats.set(s);

      this.statusChart?.destroy();
      this.statusChart = new ApexCharts(this.statusChartEl.nativeElement, {
        series: [s.openTickets, s.inProgressTickets, s.completedTickets, s.cancelledTickets],
        chart: { type: 'donut', height: 280, fontFamily: 'inherit' },
        labels: ['Open', 'In Progress', 'Completed', 'Cancelled'],
        colors: ['#3b82f6', '#f59e0b', '#10b981', '#94a3b8'],
        stroke: { width: 0 },
        legend: { position: 'bottom', fontSize: '12px', markers: { size: 6 } },
        dataLabels: { enabled: false },
        plotOptions: {
          pie: {
            donut: {
              size: '72%',
              labels: {
                show: true,
                name: { show: false },
                value: {
                  show: true,
                  fontSize: '28px',
                  fontWeight: 800,
                  color: '#0f172a',
                  offsetY: -6,
                },
                total: {
                  show: true,
                  label: 'Total Tickets',
                  fontSize: '11px',
                  color: '#94a3b8',
                  formatter: () => `${s.totalTickets}`,
                },
              },
            },
          },
        },
      });
      this.statusChart.render();
    });

    this.service.getMonthlyEarnings().subscribe((data) => {
      this.earningsChart?.destroy();
      this.earningsChart = new ApexCharts(this.earningsChartEl.nativeElement, {
        series: [{ name: 'Earnings', data: data.map((d) => d.earnings) }],
        chart: {
          type: 'area',
          height: 320,
          toolbar: { show: false },
          fontFamily: 'inherit',
        },
        colors: ['#f59e0b'],
        stroke: { curve: 'smooth', width: 3 },
        fill: {
          type: 'gradient',
          gradient: {
            shadeIntensity: 1,
            opacityFrom: 0.4,
            opacityTo: 0.05,
            colorStops: [
              { offset: 0, color: '#f59e0b', opacity: 0.4 },
              { offset: 100, color: '#fff7ed', opacity: 0.0 },
            ],
          },
        },
        dataLabels: { enabled: false },
        markers: { size: 5, colors: ['#fff'], strokeColors: '#f59e0b', strokeWidth: 3 },
        xaxis: {
          categories: data.map((d) => d.month),
          labels: { style: { colors: '#94a3b8', fontSize: '12px' } },
          axisBorder: { show: false },
          axisTicks: { show: false },
        },
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
        },
        tooltip: {
          theme: 'light',
          y: { formatter: (val: number) => '$' + val.toLocaleString() },
        },
      });
      this.earningsChart.render();
    });

    this.service.getCategoryEarnings().subscribe((c) => this.categories.set(c));
    this.service.getRecentTickets().subscribe((t) => this.recentTickets.set(t));
  }

  formatMoney(n: number): string {
    return '$' + n.toLocaleString('en-US');
  }

  statusLabel(s: TicketStatus): string {
    return {
      open: 'Open',
      in_progress: 'In Progress',
      completed: 'Completed',
      cancelled: 'Cancelled',
    }[s];
  }

  paymentLabel(p: PaymentStatus): string {
    return { pending: 'Pending', paid: 'Paid', overdue: 'Overdue' }[p];
  }

  timeAgo(date: string): string {
    const diff = (Date.now() - new Date(date).getTime()) / 1000;
    if (diff < 60) return 'Just now';
    if (diff < 3600) return `${Math.floor(diff / 60)} min ago`;
    if (diff < 86400) return `${Math.floor(diff / 3600)} hr ago`;
    return `${Math.floor(diff / 86400)} day(s) ago`;
  }

  totalCategoryEarnings(): number {
    return this.categories().reduce((sum, c) => sum + c.totalEarnings, 0);
  }

  categoryPercent(c: CategoryEarnings): number {
    const total = this.totalCategoryEarnings();
    return total > 0 ? (c.totalEarnings / total) * 100 : 0;
  }
}
