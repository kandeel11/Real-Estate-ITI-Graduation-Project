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
import { DecimalPipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';

// مسارات السيرفيس والموديلز (تأكد إنها مطابقة لمشروعك)
import { AdminDashboardService } from '../../Services/admin-dashboard';
import { UserService } from '../../Services/user.service';
import { PropertyService } from '../../Services/property.service';
import { DashboardStats, PendingProperty, RentPayment, AdminUser } from '../../Models';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [NgApexchartsModule, DecimalPipe, TranslatePipe],
  templateUrl: './dashboard.html',
  styleUrls: ['./dashboard.scss'],
})
export class DashboardComponent {
  private service = inject(AdminDashboardService);
  private propertyService = inject(PropertyService);
  private UserService = inject(UserService);

  // ===== State Signals =====
  stats = signal<DashboardStats | null>(null);
  pendingProperties = signal<PendingProperty[]>([]);
  recentPayments = signal<RentPayment[]>([]);
  latestUsers = signal<AdminUser[]>([]);

  // ===== Charts State =====
  financialChart: any = null;
  occupancyChart: any = null;

  // ===== Occupancy State =====
  occupiedCount = signal(0);
  vacantCount = signal(0);
  occupancyPercent = signal(0);
  // ===== Properties Stats State =====
  totalActiveCount = signal(0);
  regionsCount = signal(0);
  // ===== Users Stats State =====
  // ===== Users Roles Chart State =====
  usersChart: any = null;
  totalUsersCount = signal(0);
  // دي Signal هتشيل الداتا بتاعت المفتاح (الاسم، العدد، اللون) عشان نعرضها في الـ HTML
  userRolesLegend = signal<{ label: string; count: number; color: string }[]>([]);
  constructor() {
    this.loadAll();
  }

  loadAll() {
    // --- Users Chart Setup (Active vs Banned) ---
    // بنطلب حجم صفحة كبير مؤقتاً عشان نحسب الإحصائيات بشكل تقريبي لغاية ما يتوفر API للإحصائيات
    // --- Users Chart Setup (By Roles) ---
    this.UserService.getUsers({ page: 1, pageSize: 1000 }).subscribe((res) => {
      const users = res.items || [];
      const total = res.total || users.length;
      this.totalUsersCount.set(total);

      // تعريف الأدوار والألوان المتناسقة
      const roleKeys = [
        'admin',
        'owner',
        'tenant',
        'broker',
        'company_manager',
        'maintenance_company',
      ];
      const roleLabels = ['Admin', 'Owner', 'Tenant', 'Broker', 'Manager', 'Maintenance'];
      const roleColors = ['#6366f1', '#10b981', '#f59e0b', '#8b5cf6', '#ec4899', '#0ea5e9']; // 6 ألوان مختلفة

      // حساب العدد لكل دور
      const counts = roleKeys.map((role) => users.filter((u: any) => u.role === role).length);

      // تجهيز بيانات المفتاح (Legend) للـ HTML (هنفلتر الأصفار عشان متبقاش زحمة لو دور معين مفيهوش حد)
      const legendData = roleKeys
        .map((key, index) => ({
          label: roleLabels[index],
          count: counts[index],
          color: roleColors[index],
        }))
        .filter((item) => item.count > 0);

      this.userRolesLegend.set(legendData);

      // بناء الـ Chart
      this.usersChart = {
        series: legendData.map((item) => item.count), // نبعت الأرقام اللي مش بصفر بس
        labels: legendData.map((item) => item.label), // نبعت الأسماء المقابلة ليها
        colors: legendData.map((item) => item.color), // نبعت الألوان المقابلة
        chart: {
          type: 'donut',
          height: 280,
          fontFamily: 'inherit',
          sparkline: { enabled: false },
        } as ApexChart,
        stroke: { width: 0 } as ApexStroke,
        legend: { show: false } as ApexLegend,
        dataLabels: { enabled: false } as ApexDataLabels,
        tooltip: {
          y: { formatter: (val: number) => `${val} Users` },
        } as ApexTooltip,
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
                  formatter: () => `${this.totalUsersCount()}`,
                },
                total: {
                  show: true,
                  label: 'Total Users',
                  fontSize: '13px',
                  fontWeight: 500,
                  color: '#94a3b8',
                  formatter: () => `${this.totalUsersCount()}`,
                },
              },
            },
          },
        } as ApexPlotOptions,
      };
    });
    // 1. Get General Stats
    this.service.getStats().subscribe((s) => this.stats.set(s));

    // 2. Financial Chart Setup
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

    // 3. Occupancy Chart Setup (Dynamic Data)
    this.propertyService.getAllProperties().subscribe((properties) => {
      // 1. فلترة العقارات بناءً على الـ Status بتاعتك
      // ملحوظة: تأكد إن كلمة 'Rented' و 'Active' مكتوبين بنفس الطريقة اللي راجعة من الـ API
      // (ممكن يكونوا أرقام زي 1 و 2 لو إنت عاملهم Enum)
      const occupied = properties.filter((p: any) => p.status === 'Rented').length;
      const vacant = properties.filter((p: any) => p.status === 'Active').length;

      // 2. إجمالي العقارات المعروضة للإيجار (عشان نطلع النسبة صح)
      const totalMarketableProperties = occupied + vacant || 1; // || 1 عشان نمنع القسمة على صفر لو مفيش عقارات

      // 3. حساب النسبة المئوية للإشغال
      const percent = Math.round((occupied / totalMarketableProperties) * 100);

      this.occupiedCount.set(occupied);
      this.vacantCount.set(vacant);
      this.occupancyPercent.set(percent);

      this.occupancyChart = {
        series: [occupied, vacant] as ApexNonAxisChartSeries,
        chart: {
          type: 'donut',
          height: 280,
          fontFamily: 'inherit',
          sparkline: { enabled: false },
        } as ApexChart,
        labels: ['Occupied (Rented)', 'Vacant (Active)'], // غيرنا الأسماء عشان توضح أكتر
        colors: ['#4f46e5', '#e0e7ff'],
        stroke: { width: 0 } as ApexStroke,
        legend: { show: false } as ApexLegend,
        dataLabels: { enabled: false } as ApexDataLabels,
        tooltip: {
          y: { formatter: (val: number) => `${val} Units` },
        } as ApexTooltip,
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
                  formatter: () => `${this.occupancyPercent()}%`,
                },
                total: {
                  show: true,
                  label: 'Occupancy Rate',
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
      const activeProperties = properties.filter(
        (p: any) => p.status === 'Active' || p.status === 'Rented',
      );
      this.totalActiveCount.set(activeProperties.length);

      const uniqueRegions = new Set(activeProperties.map((p: any) => p.city || p.region));
      this.regionsCount.set(uniqueRegions.size);
    });

    // 4. Recent Payments
    this.service.getRecentPayments().subscribe((data) => this.recentPayments.set(data.slice(0, 3)));

    // uncomment if you need pending properties here:
    // this.service.getPendingProperties().subscribe((data) => this.pendingProperties.set(data.slice(0, 2)));
  }

  // ===== Actions =====
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

  // ===== Helpers =====
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
